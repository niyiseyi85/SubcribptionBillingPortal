using FluentAssertions;
using FluentValidation.TestHelper;
using SubscriptionBillingPortal.Application.Features.Customers.Commands.CreateCustomer;
using SubscriptionBillingPortal.Application.Features.Invoices.Commands.PayInvoice;
using SubscriptionBillingPortal.Application.Features.Invoices.Queries.GetInvoices;
using SubscriptionBillingPortal.Application.Features.Subscriptions.Commands.ActivateSubscription;
using SubscriptionBillingPortal.Application.Features.Subscriptions.Commands.CancelSubscription;
using SubscriptionBillingPortal.Application.Features.Subscriptions.Commands.CreateSubscription;

namespace SubscriptionBillingPortal.UnitTests.Application.Validation;

/// <summary>
/// Unit tests for all FluentValidation command validators.
/// No infrastructure — validators are pure in-memory logic.
/// </summary>
public sealed class CommandValidatorTests
{
    // ── CreateCustomer ────────────────────────────────────────────────────────

    [Fact]
    public void CreateCustomerCommandValidator_WithValidData_ShouldPassValidation()
    {
        var result = new CreateCustomerCommandValidator()
            .TestValidate(new CreateCustomerCommand("Jane", "Doe", "jane@example.com", Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateCustomerCommandValidator_WithEmptyFirstName_ShouldFailValidation()
    {
        var result = new CreateCustomerCommandValidator()
            .TestValidate(new CreateCustomerCommand(string.Empty, "Doe", "jane@example.com", Guid.NewGuid()));

        result.ShouldHaveValidationErrorFor(c => c.FirstName);
    }

    [Fact]
    public void CreateCustomerCommandValidator_WithEmptyLastName_ShouldFailValidation()
    {
        var result = new CreateCustomerCommandValidator()
            .TestValidate(new CreateCustomerCommand("Jane", string.Empty, "jane@example.com", Guid.NewGuid()));

        result.ShouldHaveValidationErrorFor(c => c.LastName);
    }

    [Fact]
    public void CreateCustomerCommandValidator_WithInvalidEmail_ShouldFailValidation()
    {
        var result = new CreateCustomerCommandValidator()
            .TestValidate(new CreateCustomerCommand("Jane", "Doe", "not-an-email", Guid.NewGuid()));

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void CreateCustomerCommandValidator_WithEmptyIdempotencyKey_ShouldFailValidation()
    {
        var result = new CreateCustomerCommandValidator()
            .TestValidate(new CreateCustomerCommand("Jane", "Doe", "jane@example.com", Guid.Empty));

        result.ShouldHaveValidationErrorFor(c => c.IdempotencyKey);
    }

    // ── CreateSubscription ────────────────────────────────────────────────────

    [Fact]
    public void CreateSubscriptionCommandValidator_WithValidData_ShouldPassValidation()
    {
        var result = new CreateSubscriptionCommandValidator()
            .TestValidate(new CreateSubscriptionCommand(Guid.NewGuid(), "Pro", "Monthly", Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateSubscriptionCommandValidator_WithEmptyCustomerId_ShouldFailValidation()
    {
        var result = new CreateSubscriptionCommandValidator()
            .TestValidate(new CreateSubscriptionCommand(Guid.Empty, "Pro", "Monthly", Guid.NewGuid()));

        result.ShouldHaveValidationErrorFor(c => c.CustomerId);
    }

    [Fact]
    public void CreateSubscriptionCommandValidator_WithInvalidPlanType_ShouldFailValidation()
    {
        var result = new CreateSubscriptionCommandValidator()
            .TestValidate(new CreateSubscriptionCommand(Guid.NewGuid(), "Platinum", "Monthly", Guid.NewGuid()));

        result.ShouldHaveValidationErrorFor(c => c.PlanType);
    }

    [Fact]
    public void CreateSubscriptionCommandValidator_WithInvalidBillingInterval_ShouldFailValidation()
    {
        var result = new CreateSubscriptionCommandValidator()
            .TestValidate(new CreateSubscriptionCommand(Guid.NewGuid(), "Pro", "Weekly", Guid.NewGuid()));

        result.ShouldHaveValidationErrorFor(c => c.BillingInterval);
    }

    // ── ActivateSubscription ──────────────────────────────────────────────────

    [Fact]
    public void ActivateSubscriptionCommandValidator_WithValidData_ShouldPassValidation()
    {
        var result = new ActivateSubscriptionCommandValidator()
            .TestValidate(new ActivateSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ActivateSubscriptionCommandValidator_WithEmptySubscriptionId_ShouldFailValidation()
    {
        var result = new ActivateSubscriptionCommandValidator()
            .TestValidate(new ActivateSubscriptionCommand(Guid.Empty, Guid.NewGuid()));

        result.ShouldHaveValidationErrorFor(c => c.SubscriptionId);
    }

    // ── CancelSubscription ────────────────────────────────────────────────────

    [Fact]
    public void CancelSubscriptionCommandValidator_WithValidData_ShouldPassValidation()
    {
        var result = new CancelSubscriptionCommandValidator()
            .TestValidate(new CancelSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    // ── PayInvoice ────────────────────────────────────────────────────────────

    [Fact]
    public void PayInvoiceCommandValidator_WithValidData_ShouldPassValidation()
    {
        var result = new PayInvoiceCommandValidator()
            .TestValidate(new PayInvoiceCommand(Guid.NewGuid(), Guid.NewGuid(), "ref-001", Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void PayInvoiceCommandValidator_WithEmptyInvoiceId_ShouldFailValidation()
    {
        var result = new PayInvoiceCommandValidator()
            .TestValidate(new PayInvoiceCommand(Guid.Empty, Guid.NewGuid(), "ref-001", Guid.NewGuid()));

        result.ShouldHaveValidationErrorFor(c => c.InvoiceId);
    }

    [Fact]
    public void PayInvoiceCommandValidator_WithEmptySubscriptionId_ShouldFailValidation()
    {
        var result = new PayInvoiceCommandValidator()
            .TestValidate(new PayInvoiceCommand(Guid.NewGuid(), Guid.Empty, "ref-001", Guid.NewGuid()));

        result.ShouldHaveValidationErrorFor(c => c.SubscriptionId);
    }

    [Fact]
    public void PayInvoiceCommandValidator_WithEmptyPaymentReference_ShouldFailValidation()
    {
        var result = new PayInvoiceCommandValidator()
            .TestValidate(new PayInvoiceCommand(Guid.NewGuid(), Guid.NewGuid(), string.Empty, Guid.NewGuid()));

        result.ShouldHaveValidationErrorFor(c => c.PaymentReference);
    }

    // ── GetInvoices ───────────────────────────────────────────────────────────

    [Fact]
    public void GetInvoicesQueryValidator_WithValidData_ShouldPassValidation()
    {
        var result = new GetInvoicesQueryValidator()
            .TestValidate(new GetInvoicesQuery(Guid.NewGuid(), PageNumber: 1, PageSize: 20));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void GetInvoicesQueryValidator_WithEmptySubscriptionId_ShouldFailValidation()
    {
        var result = new GetInvoicesQueryValidator()
            .TestValidate(new GetInvoicesQuery(Guid.Empty, PageNumber: 1, PageSize: 20));

        result.ShouldHaveValidationErrorFor(q => q.SubscriptionId);
    }

    [Fact]
    public void GetInvoicesQueryValidator_WithZeroPageNumber_ShouldFailValidation()
    {
        var result = new GetInvoicesQueryValidator()
            .TestValidate(new GetInvoicesQuery(Guid.NewGuid(), PageNumber: 0, PageSize: 20));

        result.ShouldHaveValidationErrorFor(q => q.PageNumber);
    }

    [Fact]
    public void GetInvoicesQueryValidator_WithPageSizeOverLimit_ShouldFailValidation()
    {
        var result = new GetInvoicesQueryValidator()
            .TestValidate(new GetInvoicesQuery(Guid.NewGuid(), PageNumber: 1, PageSize: 101));

        result.ShouldHaveValidationErrorFor(q => q.PageSize);
    }

    [Fact]
    public void GetInvoicesQueryValidator_WithZeroPageSize_ShouldFailValidation()
    {
        var result = new GetInvoicesQueryValidator()
            .TestValidate(new GetInvoicesQuery(Guid.NewGuid(), PageNumber: 1, PageSize: 0));

        result.ShouldHaveValidationErrorFor(q => q.PageSize);
    }

    // ── CancelSubscription (missing edge cases) ───────────────────────────────

    [Fact]
    public void CancelSubscriptionCommandValidator_WithEmptySubscriptionId_ShouldFailValidation()
    {
        var result = new CancelSubscriptionCommandValidator()
            .TestValidate(new CancelSubscriptionCommand(Guid.Empty, Guid.NewGuid()));

        result.ShouldHaveValidationErrorFor(c => c.SubscriptionId);
    }

    [Fact]
    public void CancelSubscriptionCommandValidator_WithEmptyIdempotencyKey_ShouldFailValidation()
    {
        var result = new CancelSubscriptionCommandValidator()
            .TestValidate(new CancelSubscriptionCommand(Guid.NewGuid(), Guid.Empty));

        result.ShouldHaveValidationErrorFor(c => c.IdempotencyKey);
    }

    // ── ActivateSubscription (missing edge cases) ─────────────────────────────

    [Fact]
    public void ActivateSubscriptionCommandValidator_WithEmptyIdempotencyKey_ShouldFailValidation()
    {
        var result = new ActivateSubscriptionCommandValidator()
            .TestValidate(new ActivateSubscriptionCommand(Guid.NewGuid(), Guid.Empty));

        result.ShouldHaveValidationErrorFor(c => c.IdempotencyKey);
    }
}
