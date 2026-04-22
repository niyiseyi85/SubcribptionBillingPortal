using FluentAssertions;
using SubscriptionBillingPortal.Domain.Aggregates;
using SubscriptionBillingPortal.Domain.Exceptions;
using SubscriptionBillingPortal.Domain.ValueObjects;

namespace SubscriptionBillingPortal.Domain.Tests.Aggregates;

/// <summary>
/// Domain tests for the Customer aggregate.
/// </summary>
public sealed class CustomerTests
{
    [Fact]
    public void Create_WithValidNamesAndEmail_ShouldReturnCustomer()
    {
        // Act
        var customer = Customer.Create("Jane", "Doe", "jane@example.com");

        // Assert
        customer.Should().NotBeNull();
        customer.Id.Should().NotBeEmpty();
        customer.FirstName.Should().Be("Jane");
        customer.LastName.Should().Be("Doe");
        customer.FullName.Should().Be("Jane Doe");
        customer.Email.Value.Should().Be("jane@example.com");
    }

    [Fact]
    public void Create_WithEmptyFirstName_ShouldThrowDomainException()
    {
        // Act
        var act = () => Customer.Create(string.Empty, "Doe", "jane@example.com");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*first name cannot be empty*");
    }

    [Fact]
    public void Create_WithEmptyLastName_ShouldThrowDomainException()
    {
        // Act
        var act = () => Customer.Create("Jane", string.Empty, "jane@example.com");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*last name cannot be empty*");
    }

    [Fact]
    public void Create_WithEmptyEmail_ShouldThrowDomainException()
    {
        // Act
        var act = () => Customer.Create("Jane", "Doe", string.Empty);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*email cannot be empty*");
    }

    [Fact]
    public void Create_ShouldNormalizeEmailToLowercase()
    {
        // Act
        var customer = Customer.Create("Jane", "Doe", "JANE@EXAMPLE.COM");

        // Assert
        customer.Email.Value.Should().Be("jane@example.com");
    }

    [Fact]
    public void Create_WithInvalidEmailFormat_ShouldThrowDomainException()
    {
        // Act
        var act = () => Customer.Create("Jane", "Doe", "not-an-email");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*not a valid email*");
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var customer = Customer.Create("Jane", "Doe", "jane@example.com");

        // Assert
        customer.CreatedAt.Should().BeOnOrAfter(before);
    }
}
