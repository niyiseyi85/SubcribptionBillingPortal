using MediatR;
using SubscriptionBillingPortal.API.Requests;
using SubscriptionBillingPortal.Application.Features.Customers.Commands.CreateCustomer;
using SubscriptionBillingPortal.Shared.Responses;

namespace SubscriptionBillingPortal.API.Endpoints;

/// <summary>
/// Customer endpoint definitions.
/// All responses are wrapped in <see cref="ApiResponse{T}"/> for a consistent consumer contract.
/// All business logic is delegated to MediatR command/query handlers.
/// </summary>
public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/customers")
            .WithTags("Customers");

        group.MapPost("/", async (
            CreateCustomerRequest request,
            HttpContext httpContext,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var idempotencyKey = GetOrCreateIdempotencyKey(httpContext);

            var command = new CreateCustomerCommand(
                FirstName: request.FirstName,
                LastName: request.LastName,
                Email: request.Email,
                IdempotencyKey: idempotencyKey);

            var customer = await sender.Send(command, cancellationToken);

            var response = ApiResponse<object>.Created(customer, "Customer created successfully.");
            return Results.Created($"/customers/{customer.Id}", response);
        })
        .WithName("CreateCustomer")
        .WithSummary("Create a new customer")
        .Produces<ApiResponse<object>>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict);
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

