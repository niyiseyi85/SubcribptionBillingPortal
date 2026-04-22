using MediatR;
using SubscriptionBillingPortal.API.Requests;
using SubscriptionBillingPortal.Application.DTOs;
using SubscriptionBillingPortal.Application.Features.Invoices.Commands.PayInvoice;
using SubscriptionBillingPortal.Application.Features.Invoices.Queries.GetInvoices;
using SubscriptionBillingPortal.Shared.Pagination;
using SubscriptionBillingPortal.Shared.Responses;

namespace SubscriptionBillingPortal.API.Endpoints;

/// <summary>
/// Invoice endpoint definitions.
/// GET /invoices supports mandatory pagination via query string parameters.
/// All responses are wrapped in <see cref="ApiResponse{T}"/> for a consistent consumer contract.
/// </summary>
public static class InvoiceEndpoints
{
    public static void MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/invoices")
            .WithTags("Invoices");

        group.MapPost("/{id:guid}/pay", async (
            Guid id,
            PayInvoiceRequest request,
            HttpContext httpContext,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var idempotencyKey = GetOrCreateIdempotencyKey(httpContext);

            var command = new PayInvoiceCommand(
                InvoiceId: id,
                SubscriptionId: request.SubscriptionId,
                PaymentReference: request.PaymentReference,
                IdempotencyKey: idempotencyKey);

            var invoice = await sender.Send(command, cancellationToken);

            var response = ApiResponse<object>.Ok(invoice, "Invoice paid successfully.");
            return Results.Ok(response);
        })
        .WithName("PayInvoice")
        .WithSummary("Pay a specific invoice")
        .Produces<ApiResponse<object>>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/", async (
            Guid subscriptionId,
            int pageNumber,
            int pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var query = new GetInvoicesQuery(
                SubscriptionId: subscriptionId,
                PageNumber: pageNumber <= 0 ? 1 : pageNumber,
                PageSize: pageSize <= 0 ? 20 : pageSize);

            var pagedResult = await sender.Send(query, cancellationToken);

            var response = ApiResponse<PaginatedResult<InvoiceDto>>.Ok(
                pagedResult,
                $"Retrieved {pagedResult.Items.Count} invoice(s) (page {pagedResult.PageNumber} of {pagedResult.TotalPages}).");

            return Results.Ok(response);
        })
        .WithName("GetInvoices")
        .WithSummary("Get paginated invoices for a subscription")
        .Produces<ApiResponse<PaginatedResult<InvoiceDto>>>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static Guid GetOrCreateIdempotencyKey(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("Idempotency-Key", out var keyHeader)
            && Guid.TryParse(keyHeader, out var parsedKey))
        {
            return parsedKey;
        }
        return Guid.NewGuid();
    }
}

