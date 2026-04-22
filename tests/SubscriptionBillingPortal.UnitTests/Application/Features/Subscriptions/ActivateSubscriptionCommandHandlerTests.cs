using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;
using SubscriptionBillingPortal.Application.Contracts.Persistence;
using SubscriptionBillingPortal.Application.Contracts.Services;
using SubscriptionBillingPortal.Application.DTOs;
using SubscriptionBillingPortal.Application.Features.Subscriptions.Commands.ActivateSubscription;
using SubscriptionBillingPortal.Application.Mappings;
using SubscriptionBillingPortal.Domain.Aggregates;
using SubscriptionBillingPortal.Domain.Enums;
using SubscriptionBillingPortal.Domain.ValueObjects;

namespace SubscriptionBillingPortal.UnitTests.Application.Features.Subscriptions;

/// <summary>
/// Unit tests for ActivateSubscriptionCommandHandler.
/// All dependencies are mocked — no infrastructure involved.
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
        var subscription = Subscription.Create(Guid.NewGuid(), SubscriptionPlan.Create(PlanType.Pro, BillingInterval.Monthly));
        _subscriptionRepositoryMock
            .Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var result = await _handler.Handle(new ActivateSubscriptionCommand(subscription.Id, Guid.NewGuid()), CancellationToken.None);

        result.Should().NotBeNull();
        result.Status.Should().Be(SubscriptionStatus.Active.ToString());
    }

    [Fact]
    public async Task Handle_WithValidInactiveSubscription_ShouldSaveChanges()
    {
        var subscription = Subscription.Create(Guid.NewGuid(), SubscriptionPlan.Create(PlanType.Pro, BillingInterval.Monthly));
        _subscriptionRepositoryMock
            .Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        await _handler.Handle(new ActivateSubscriptionCommand(subscription.Id, Guid.NewGuid()), CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSubscriptionNotFound_ShouldThrowKeyNotFoundException()
    {
        var subscriptionId = Guid.NewGuid();
        _subscriptionRepositoryMock
            .Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var act = () => _handler.Handle(new ActivateSubscriptionCommand(subscriptionId, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{subscriptionId}*");
    }

    [Fact]
    public async Task Handle_WhenIdempotencyKeyAlreadyProcessed_ShouldReturnCachedResponse()
    {
        var cachedDto = new SubscriptionDto(
            Guid.NewGuid(), Guid.NewGuid(), "Pro", "Monthly", 29.99m, "Active",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null);
        _idempotencyServiceMock
            .Setup(s => s.HasBeenProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _idempotencyServiceMock
            .Setup(s => s.GetResponseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(cachedDto));

        var result = await _handler.Handle(new ActivateSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(cachedDto.Id);
        result.Status.Should().Be("Active");
    }
}
