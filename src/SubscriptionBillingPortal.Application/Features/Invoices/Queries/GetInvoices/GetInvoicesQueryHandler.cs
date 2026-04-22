using MediatR;
using Microsoft.Extensions.Logging;
using Mapster;
using SubscriptionBillingPortal.Application.Contracts.Persistence;
using SubscriptionBillingPortal.Application.DTOs;
using SubscriptionBillingPortal.Shared.Pagination;

namespace SubscriptionBillingPortal.Application.Features.Invoices.Queries.GetInvoices;

public sealed class GetInvoicesQueryHandler : IRequestHandler<GetInvoicesQuery, PaginatedResult<InvoiceDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetInvoicesQueryHandler> _logger;

    public GetInvoicesQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetInvoicesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PaginatedResult<InvoiceDto>> Handle(GetInvoicesQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Retrieving invoices for subscription '{SubscriptionId}' — page {PageNumber} of size {PageSize}",
            query.SubscriptionId,
            query.PageNumber,
            query.PageSize);

        var subscription = await _unitOfWork.Subscriptions.GetByIdAsync(query.SubscriptionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Subscription '{query.SubscriptionId}' was not found.");
        var allInvoices = subscription.Invoices
            .OrderByDescending(i => i.IssuedAt)
            .ToList();

        var totalCount = allInvoices.Count;

        var pagedInvoices = allInvoices
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(i => i.Adapt<InvoiceDto>());

        var result = PaginatedResult<InvoiceDto>.Create(pagedInvoices, totalCount, query.PageNumber, query.PageSize);

        _logger.LogInformation(
            "Retrieved {ItemCount} of {TotalCount} invoice(s) for subscription '{SubscriptionId}'",
            result.Items.Count,
            totalCount,
            query.SubscriptionId);

        return result;
    }
}
