using FluentAssertions;
using SubscriptionBillingPortal.Domain.Aggregates;
using SubscriptionBillingPortal.Domain.Exceptions;

namespace SubscriptionBillingPortal.UnitTests.Domain;

/// <summary>
/// Unit tests for the Customer aggregate.
/// Pure domain — no infrastructure dependencies.
/// </summary>
public sealed class CustomerTests
{
    [Fact]
    public void Create_WithValidNamesAndEmail_ShouldReturnCustomer()
    {
        var customer = Customer.Create("Jane", "Doe", "jane@example.com");

        customer.Should().NotBeNull();
        customer.Id.Should().NotBeEmpty();
        customer.FirstName.Should().Be("Jane");
        customer.LastName.Should().Be("Doe");
        customer.FullName.Should().Be("Jane Doe");
        customer.Email.Should().Be("jane@example.com");
    }

    [Fact]
    public void Create_WithEmptyFirstName_ShouldThrowDomainException()
    {
        var act = () => Customer.Create(string.Empty, "Doe", "jane@example.com");

        act.Should().Throw<DomainException>()
            .WithMessage("*first name cannot be empty*");
    }

    [Fact]
    public void Create_WithEmptyLastName_ShouldThrowDomainException()
    {
        var act = () => Customer.Create("Jane", string.Empty, "jane@example.com");

        act.Should().Throw<DomainException>()
            .WithMessage("*last name cannot be empty*");
    }

    [Fact]
    public void Create_WithEmptyEmail_ShouldThrowDomainException()
    {
        var act = () => Customer.Create("Jane", "Doe", string.Empty);

        act.Should().Throw<DomainException>()
            .WithMessage("*email cannot be empty*");
    }

    [Fact]
    public void Create_ShouldNormalizeEmailToLowercase()
    {
        var customer = Customer.Create("Jane", "Doe", "JANE@EXAMPLE.COM");

        customer.Email.Should().Be("jane@example.com");
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        var before = DateTimeOffset.UtcNow;

        var customer = Customer.Create("Jane", "Doe", "jane@example.com");

        customer.CreatedAt.Should().BeOnOrAfter(before);
    }
}
