using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;
using SubscriptionBillingPortal.Application.Contracts.Persistence;
using SubscriptionBillingPortal.Application.Contracts.Services;
using SubscriptionBillingPortal.Application.Features.Invoices.Commands.PayInvoice;
using SubscriptionBillingPortal.Application.Mappings;
using SubscriptionBillingPortal.Domain.Aggregates;
using SubscriptionBillingPortal.Domain.Enums;
using SubscriptionBillingPortal.Domain.Exceptions;
using SubscriptionBillingPortal.Domain.ValueObjects;

namespace SubscriptionBillingPortal.Application.Tests.Features.Invoices;

/// <summary>
/// Application-layer tests for PayInvoiceCommandHandler.
/// Domain events now flow through the Outbox (UnitOfWork) — no direct dispatcher.
/// </summary>
public sealed class PayInvoiceCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ISubscriptionRepository> _subscriptionRepositoryMock;
    private readonly Mock<IIdempotencyService> _idempotencyServiceMock;
    private readonly PayInvoiceCommandHandler _handler;

    public PayInvoiceCommandHandlerTests()
    {
        MappingConfiguration.Configure();

        _subscriptionRepositoryMock = new Mock<ISubscriptionRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.Subscriptions).Returns(_subscriptionRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _idempotencyServiceMock = new Mock<IIdempotencyService>();
        _idempotencyServiceMock
            .Setup(s => s.HasBeenProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _handler = new PayInvoiceCommandHandler(
            _unitOfWorkMock.Object,
            _idempotencyServiceMock.Object,
            NullLogger<PayInvoiceCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WithValidPendingInvoice_ShouldReturnPaidInvoiceDto()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), SubscriptionPlan.Create(PlanType.Pro, BillingInterval.Monthly));
        subscription.Activate();
        var invoice = subscription.Invoices.First();

        _subscriptionRepositoryMock
            .Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var command = new PayInvoiceCommand(invoice.Id, subscription.Id, "ref-001", Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(invoice.Id);
        result.Status.Should().Be(InvoiceStatus.Paid.ToString());
        result.PaymentReference.Should().Be("ref-001");
    }

    [Fact]
    public async Task Handle_WithValidPendingInvoice_ShouldSaveChanges()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), SubscriptionPlan.Create(PlanType.Pro, BillingInterval.Monthly));
        subscription.Activate();
        var invoice = subscription.Invoices.First();

        _subscriptionRepositoryMock
            .Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var command = new PayInvoiceCommand(invoice.Id, subscription.Id, "ref-001", Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — domain events flow through UnitOfWork into the Outbox, not dispatched directly
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenInvoiceAlreadyPaidWithDifferentReference_ShouldThrowDomainException()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), SubscriptionPlan.Create(PlanType.Pro, BillingInterval.Monthly));
        subscription.Activate();
        var invoice = subscription.Invoices.First();
        subscription.PayInvoice(invoice.Id, "ref-001"); // paid with ref-001
        subscription.ClearDomainEvents();

        _subscriptionRepositoryMock
            .Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        // Attempt with a different reference — must throw
        var command = new PayInvoiceCommand(invoice.Id, subscription.Id, "ref-002", Guid.NewGuid());

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*already been paid*");
    }

    [Fact]
    public async Task Handle_WhenSubscriptionNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        _subscriptionRepositoryMock
            .Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var command = new PayInvoiceCommand(Guid.NewGuid(), subscriptionId, "ref-001", Guid.NewGuid());

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{subscriptionId}*");
    }

    [Fact]
    public async Task Handle_WhenIdempotencyKeyAlreadyProcessed_ShouldReturnCachedResponse()
    {
        // Arrange
        var cachedDto = new DTOs.InvoiceDto(
            Guid.NewGuid(), Guid.NewGuid(), 29.99m, "Paid",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "ref-001");
        _idempotencyServiceMock
            .Setup(s => s.HasBeenProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _idempotencyServiceMock
            .Setup(s => s.GetResponseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(cachedDto));

        var command = new PayInvoiceCommand(Guid.NewGuid(), Guid.NewGuid(), "ref-001", Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(cachedDto.Id);
        result.Status.Should().Be("Paid");
    }
}
