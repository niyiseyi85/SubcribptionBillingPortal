using MediatR;
using SubscriptionBillingPortal.API.Requests;
using SubscriptionBillingPortal.Application.Features.Subscriptions.Commands.ActivateSubscription;
using SubscriptionBillingPortal.Application.Features.Subscriptions.Commands.CancelSubscription;
using SubscriptionBillingPortal.Application.Features.Subscriptions.Commands.CreateSubscription;
using SubscriptionBillingPortal.Shared.Responses;

namespace SubscriptionBillingPortal.API.Endpoints;

/// <summary>
/// Subscription endpoint definitions.
/// All responses are wrapped in <see cref="ApiResponse{T}"/> for a consistent consumer contract.
/// </summary>
public static class SubscriptionEndpoints
{
    public static void MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/subscriptions")
            .WithTags("Subscriptions");

        group.MapPost("/", async (
            CreateSubscriptionRequest request,
            HttpContext httpContext,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var idempotencyKey = GetOrCreateIdempotencyKey(httpContext);

            var command = new CreateSubscriptionCommand(
                CustomerId: request.CustomerId,
                PlanType: request.PlanType,
                BillingInterval: request.BillingInterval,
                IdempotencyKey: idempotencyKey);

            var subscription = await sender.Send(command, cancellationToken);

            var response = ApiResponse<object>.Created(subscription, "Subscription created successfully.");
            return Results.Created($"/subscriptions/{subscription.Id}", response);
        })
        .WithName("CreateSubscription")
        .WithSummary("Create a new subscription for a customer")
        .Produces<ApiResponse<object>>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/activate", async (
            Guid id,
            HttpContext httpContext,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var idempotencyKey = GetOrCreateIdempotencyKey(httpContext);

            var command = new ActivateSubscriptionCommand(
                SubscriptionId: id,
                IdempotencyKey: idempotencyKey);

            var subscription = await sender.Send(command, cancellationToken);

            var response = ApiResponse<object>.Ok(subscription, "Subscription activated successfully.");
            return Results.Ok(response);
        })
        .WithName("ActivateSubscription")
        .WithSummary("Activate a subscription")
        .Produces<ApiResponse<object>>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{id:guid}/cancel", async (
            Guid id,
            HttpContext httpContext,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var idempotencyKey = GetOrCreateIdempotencyKey(httpContext);

            var command = new CancelSubscriptionCommand(
                SubscriptionId: id,
                IdempotencyKey: idempotencyKey);

            var subscription = await sender.Send(command, cancellationToken);

            var response = ApiResponse<object>.Ok(subscription, "Subscription cancelled successfully.");
            return Results.Ok(response);
        })
        .WithName("CancelSubscription")
        .WithSummary("Cancel a subscription")
        .Produces<ApiResponse<object>>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
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

