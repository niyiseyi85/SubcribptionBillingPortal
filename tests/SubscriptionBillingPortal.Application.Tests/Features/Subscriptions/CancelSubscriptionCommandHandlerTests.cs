using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;
using SubscriptionBillingPortal.Application.Contracts.Persistence;
using SubscriptionBillingPortal.Application.Contracts.Services;
using SubscriptionBillingPortal.Application.Features.Subscriptions.Commands.CancelSubscription;
using SubscriptionBillingPortal.Application.Mappings;
using SubscriptionBillingPortal.Domain.Aggregates;
using SubscriptionBillingPortal.Domain.Enums;
using SubscriptionBillingPortal.Domain.Exceptions;
using SubscriptionBillingPortal.Domain.ValueObjects;

namespace SubscriptionBillingPortal.Application.Tests.Features.Subscriptions;

/// <summary>
/// Application-layer tests for CancelSubscriptionCommandHandler.
/// Uses Moq to isolate the handler from infrastructure dependencies.
/// </summary>
public sealed class CancelSubscriptionCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ISubscriptionRepository> _subscriptionRepositoryMock;
    private readonly Mock<IIdempotencyService> _idempotencyServiceMock;
    private readonly CancelSubscriptionCommandHandler _handler;

    public CancelSubscriptionCommandHandlerTests()
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

        _handler = new CancelSubscriptionCommandHandler(
            _unitOfWorkMock.Object,
            _idempotencyServiceMock.Object,
            NullLogger<CancelSubscriptionCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WithActiveSubscription_ShouldReturnCancelledSubscriptionDto()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), SubscriptionPlan.Create(PlanType.Pro, BillingInterval.Monthly));
        subscription.Activate();
        _subscriptionRepositoryMock
            .Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        // Act
        var result = await _handler.Handle(new CancelSubscriptionCommand(subscription.Id, Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(SubscriptionStatus.Cancelled.ToString());
    }

    [Fact]
    public async Task Handle_WithActiveSubscription_ShouldSaveChanges()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), SubscriptionPlan.Create(PlanType.Basic, BillingInterval.Annual));
        subscription.Activate();
        _subscriptionRepositoryMock
            .Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        // Act
        await _handler.Handle(new CancelSubscriptionCommand(subscription.Id, Guid.NewGuid()), CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSubscriptionNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        _subscriptionRepositoryMock
            .Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        // Act
        var act = () => _handler.Handle(new CancelSubscriptionCommand(subscriptionId, Guid.NewGuid()), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{subscriptionId}*");
    }

    [Fact]
    public async Task Handle_WhenSubscriptionAlreadyCancelled_ShouldThrowDomainException()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), SubscriptionPlan.Create(PlanType.Pro, BillingInterval.Monthly));
        subscription.Activate();
        subscription.Cancel();
        _subscriptionRepositoryMock
            .Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        // Act
        var act = () => _handler.Handle(new CancelSubscriptionCommand(subscription.Id, Guid.NewGuid()), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*already cancelled*");
    }

    [Fact]
    public async Task Handle_WhenIdempotencyKeyAlreadyProcessed_ShouldReturnCachedResponse()
    {
        // Arrange
        var cachedDto = new DTOs.SubscriptionDto(
            Guid.NewGuid(), Guid.NewGuid(), "Pro", "Monthly", 29.99m, "Cancelled",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        _idempotencyServiceMock
            .Setup(s => s.HasBeenProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _idempotencyServiceMock
            .Setup(s => s.GetResponseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(cachedDto));

        // Act
        var result = await _handler.Handle(new CancelSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(cachedDto.Id);
        result.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task Handle_WithActiveSubscription_ShouldMarkIdempotencyKeyAsProcessed()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), SubscriptionPlan.Create(PlanType.Pro, BillingInterval.Monthly));
        subscription.Activate();
        _subscriptionRepositoryMock
            .Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var command = new CancelSubscriptionCommand(subscription.Id, Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _idempotencyServiceMock.Verify(
            s => s.MarkAsProcessedAsync(command.IdempotencyKey, nameof(CancelSubscriptionCommand), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
