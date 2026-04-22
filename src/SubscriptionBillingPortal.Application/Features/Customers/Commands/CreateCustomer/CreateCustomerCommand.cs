using MediatR;
using SubscriptionBillingPortal.Application.DTOs;

namespace SubscriptionBillingPortal.Application.Features.Customers.Commands.CreateCustomer;

/// <summary>
/// Command to create a new customer in the system.
/// </summary>
public sealed record CreateCustomerCommand(
    string FirstName,
    string LastName,
    string Email,
    Guid IdempotencyKey) : IRequest<CustomerDto>;
