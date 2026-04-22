using MediatR;
using SubscriptionBillingPortal.Application.DTOs;
using SubscriptionBillingPortal.Shared.Pagination;

namespace SubscriptionBillingPortal.Application.Features.Invoices.Queries.GetInvoices;

/// <summary>
/// Query to retrieve invoices for a given subscription with mandatory pagination.
/// </summary>
public sealed record GetInvoicesQuery(
    Guid SubscriptionId,
    int PageNumber,
    int PageSize) : IRequest<PaginatedResult<InvoiceDto>>;
