using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SubscriptionBillingPortal.Application.Contracts.Persistence;
using SubscriptionBillingPortal.Application.Contracts.Services;
using SubscriptionBillingPortal.Application.Features.Subscriptions.Commands.ActivateSubscription;
using SubscriptionBillingPortal.Application.Mappings;
using SubscriptionBillingPortal.Domain.Aggregates;
using SubscriptionBillingPortal.Domain.Enums;
using SubscriptionBillingPortal.Domain.ValueObjects;

namespace SubscriptionBillingPortal.Application.Tests.Features.Subscriptions;

/// <summary>
/// Application-layer tests for ActivateSubscriptionCommandHandler.
/// </summary>
public sealed class ActivateSubscriptionCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ISubscriptionRepository> _subscriptionRepositoryMock;
    private readonly Mock<IIdempotencyService> _idempotencyServiceMock;
    private readonly ActivateSubscriptionCommandHandler _handler;

    public ActivateSubscriptionCommandHandlerTests()
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

        _handler = new ActivateSubscriptionCommandHandler(
            _unitOfWorkMock.Object,
            _idempotencyServiceMock.Object,
            NullLogger<ActivateSubscriptionCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WithValidInactiveSubscription_ShouldReturnActiveSubscriptionDto()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), SubscriptionPlan.Create(PlanType.Pro, BillingInterval.Monthly));
        _subscriptionRepositoryMock
            .Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var command = new ActivateSubscriptionCommand(subscription.Id, Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(SubscriptionStatus.Active.ToString());
    }

    [Fact]
    public async Task Handle_WithValidInactiveSubscription_ShouldSaveChanges()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), SubscriptionPlan.Create(PlanType.Pro, BillingInterval.Monthly));
        _subscriptionRepositoryMock
            .Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var command = new ActivateSubscriptionCommand(subscription.Id, Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — domain events flow through UnitOfWork into the Outbox, not dispatched directly
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

        var command = new ActivateSubscriptionCommand(subscriptionId, Guid.NewGuid());

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{subscriptionId}*");
    }

    [Fact]
    public async Task Handle_WhenIdempotencyKeyAlreadyProcessed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _idempotencyServiceMock
            .Setup(s => s.HasBeenProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new ActivateSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already been processed*");
    }
}
