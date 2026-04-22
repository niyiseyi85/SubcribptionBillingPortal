using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SubscriptionBillingPortal.Application.Contracts.Persistence;
using SubscriptionBillingPortal.Application.Features.Invoices.Queries.GetInvoices;
using SubscriptionBillingPortal.Application.Mappings;
using SubscriptionBillingPortal.Domain.Aggregates;
using SubscriptionBillingPortal.Domain.Enums;
using SubscriptionBillingPortal.Domain.ValueObjects;
using SubscriptionBillingPortal.Shared.Pagination;

namespace SubscriptionBillingPortal.Application.Tests.Features.Invoices;

/// <summary>
/// Application-layer tests for GetInvoicesQueryHandler.
/// </summary>
public sealed class GetInvoicesQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ISubscriptionRepository> _subscriptionRepositoryMock;
    private readonly GetInvoicesQueryHandler _handler;

    public GetInvoicesQueryHandlerTests()
    {
        MappingConfiguration.Configure();

        _subscriptionRepositoryMock = new Mock<ISubscriptionRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.Subscriptions).Returns(_subscriptionRepositoryMock.Object);

        _handler = new GetInvoicesQueryHandler(
            _unitOfWorkMock.Object,
            NullLogger<GetInvoicesQueryHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WithActiveSubscription_ShouldReturnPaginatedResult()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), SubscriptionPlan.Create(PlanType.Pro, BillingInterval.Monthly));
        subscription.Activate();                // creates invoice #1
        subscription.GenerateInvoice();         // invoice #2
        subscription.GenerateInvoice();         // invoice #3

        _subscriptionRepositoryMock
            .Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var query = new GetInvoicesQuery(subscription.Id, PageNumber: 1, PageSize: 2);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<PaginatedResult<Application.DTOs.InvoiceDto>>();
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalPages.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithPageBeyondAvailableData_ShouldReturnEmptyItems()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), SubscriptionPlan.Create(PlanType.Pro, BillingInterval.Monthly));
        subscription.Activate(); // 1 invoice

        _subscriptionRepositoryMock
            .Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var query = new GetInvoicesQuery(subscription.Id, PageNumber: 99, PageSize: 20);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenSubscriptionNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        _subscriptionRepositoryMock
            .Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var query = new GetInvoicesQuery(subscriptionId, PageNumber: 1, PageSize: 20);

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{subscriptionId}*");
    }

    [Fact]
    public async Task Handle_WithLastPage_ShouldCorrectlySetHasNextAndHasPrevious()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), SubscriptionPlan.Create(PlanType.Pro, BillingInterval.Monthly));
        subscription.Activate();        // invoice #1
        subscription.GenerateInvoice(); // invoice #2

        _subscriptionRepositoryMock
            .Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var query = new GetInvoicesQuery(subscription.Id, PageNumber: 2, PageSize: 1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
    }
}
