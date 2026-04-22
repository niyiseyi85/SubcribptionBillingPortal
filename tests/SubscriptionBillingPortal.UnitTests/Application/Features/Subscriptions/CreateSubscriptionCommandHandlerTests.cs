using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;
using SubscriptionBillingPortal.Application.Contracts.Persistence;
using SubscriptionBillingPortal.Application.Contracts.Services;
using SubscriptionBillingPortal.Application.DTOs;
using SubscriptionBillingPortal.Application.Features.Subscriptions.Commands.CreateSubscription;
using SubscriptionBillingPortal.Application.Mappings;
using SubscriptionBillingPortal.Domain.Enums;

namespace SubscriptionBillingPortal.UnitTests.Application.Features.Subscriptions;

/// <summary>
/// Unit tests for CreateSubscriptionCommandHandler.
/// All dependencies are mocked — no infrastructure involved.
/// </summary>
public sealed class CreateSubscriptionCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ISubscriptionRepository> _subscriptionRepositoryMock;
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly Mock<IIdempotencyService> _idempotencyServiceMock;
    private readonly CreateSubscriptionCommandHandler _handler;

    public CreateSubscriptionCommandHandlerTests()
    {
        MappingConfiguration.Configure();

        _subscriptionRepositoryMock = new Mock<ISubscriptionRepository>();
        _customerRepositoryMock = new Mock<ICustomerRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.Subscriptions).Returns(_subscriptionRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Customers).Returns(_customerRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _idempotencyServiceMock = new Mock<IIdempotencyService>();
        _idempotencyServiceMock
            .Setup(s => s.HasBeenProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _handler = new CreateSubscriptionCommandHandler(
            _unitOfWorkMock.Object,
            _idempotencyServiceMock.Object,
            NullLogger<CreateSubscriptionCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnSubscriptionDto()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        _customerRepositoryMock
            .Setup(r => r.ExistsAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateSubscriptionCommand(customerId, "Pro", "Monthly", Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.CustomerId.Should().Be(customerId);
        result.PlanType.Should().Be(PlanType.Pro.ToString());
        result.BillingInterval.Should().Be(BillingInterval.Monthly.ToString());
        result.Status.Should().Be(SubscriptionStatus.Inactive.ToString());
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldAddAndSave()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        _customerRepositoryMock
            .Setup(r => r.ExistsAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateSubscriptionCommand(customerId, "Basic", "Annual", Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _subscriptionRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<SubscriptionBillingPortal.Domain.Aggregates.Subscription>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCustomerNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        _customerRepositoryMock
            .Setup(r => r.ExistsAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new CreateSubscriptionCommand(customerId, "Pro", "Monthly", Guid.NewGuid());

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{customerId}*");
    }

    [Fact]
    public async Task Handle_WhenIdempotencyKeyAlreadyProcessed_ShouldReturnCachedResponse()
    {
        // Arrange
        var cachedDto = new SubscriptionDto(
            Guid.NewGuid(), Guid.NewGuid(), "Pro", "Monthly", 29.99m, "Inactive",
            DateTimeOffset.UtcNow, null, null);
        _idempotencyServiceMock
            .Setup(s => s.HasBeenProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _idempotencyServiceMock
            .Setup(s => s.GetResponseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(cachedDto));

        var command = new CreateSubscriptionCommand(Guid.NewGuid(), "Pro", "Monthly", Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(cachedDto.Id);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldMarkIdempotencyKeyAsProcessed()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        _customerRepositoryMock
            .Setup(r => r.ExistsAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateSubscriptionCommand(customerId, "Pro", "Monthly", Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _idempotencyServiceMock.Verify(
            s => s.MarkAsProcessedAsync(command.IdempotencyKey, nameof(CreateSubscriptionCommand), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
