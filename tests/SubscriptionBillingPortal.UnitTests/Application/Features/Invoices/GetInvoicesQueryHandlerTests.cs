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

namespace SubscriptionBillingPortal.UnitTests.Application.Features.Invoices;

/// <summary>
/// Unit tests for GetInvoicesQueryHandler.
/// All dependencies are mocked — no infrastructure involved.
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
        var subscription = Subscription.Create(Guid.NewGuid(), SubscriptionPlan.Create(PlanType.Pro, BillingInterval.Monthly));
        subscription.Activate();
        subscription.GenerateInvoice();
        subscription.GenerateInvoice();

        _subscriptionRepositoryMock
            .Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var result = await _handler.Handle(
            new GetInvoicesQuery(subscription.Id, PageNumber: 1, PageSize: 2), CancellationToken.None);

        result.Should().BeOfType<PaginatedResult<SubscriptionBillingPortal.Application.DTOs.InvoiceDto>>();
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(2);
        result.TotalPages.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithPageBeyondAvailableData_ShouldReturnEmptyItems()
    {
        var subscription = Subscription.Create(Guid.NewGuid(), SubscriptionPlan.Create(PlanType.Pro, BillingInterval.Monthly));
        subscription.Activate();

        _subscriptionRepositoryMock
            .Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var result = await _handler.Handle(
            new GetInvoicesQuery(subscription.Id, PageNumber: 99, PageSize: 20), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenSubscriptionNotFound_ShouldThrowKeyNotFoundException()
    {
        var subscriptionId = Guid.NewGuid();
        _subscriptionRepositoryMock
            .Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var act = () => _handler.Handle(
            new GetInvoicesQuery(subscriptionId, PageNumber: 1, PageSize: 20), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{subscriptionId}*");
    }

    [Fact]
    public async Task Handle_WithLastPage_ShouldCorrectlySetHasNextAndHasPrevious()
    {
        var subscription = Subscription.Create(Guid.NewGuid(), SubscriptionPlan.Create(PlanType.Pro, BillingInterval.Monthly));
        subscription.Activate();
        subscription.GenerateInvoice();

        _subscriptionRepositoryMock
            .Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var result = await _handler.Handle(
            new GetInvoicesQuery(subscription.Id, PageNumber: 2, PageSize: 1), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
    }
}
