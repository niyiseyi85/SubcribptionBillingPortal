using SubscriptionBillingPortal.Domain.Common;
using SubscriptionBillingPortal.Domain.Exceptions;
using SubscriptionBillingPortal.Domain.ValueObjects;

namespace SubscriptionBillingPortal.Domain.Aggregates;

/// <summary>
/// Customer aggregate root.
/// Represents a billing customer who can hold subscriptions.
/// </summary>
public sealed class Customer : AggregateRoot
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public Email Email { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private Customer() { }

    /// <summary>
    /// Factory method — the only way to create a valid Customer.
    /// </summary>
    public static Customer Create(string firstName, string lastName, string email)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new DomainException("Customer first name cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new DomainException("Customer last name cannot be empty.");
        }

        // Email.From() enforces format validation — throws DomainException on invalid input.
        var emailVo = Email.From(email);

        return new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = emailVo,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
