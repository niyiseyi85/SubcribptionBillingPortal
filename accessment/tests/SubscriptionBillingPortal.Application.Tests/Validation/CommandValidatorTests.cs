using FluentAssertions;
using FluentValidation.TestHelper;
using SubscriptionBillingPortal.Application.Features.Customers.Commands.CreateCustomer;
using SubscriptionBillingPortal.Application.Features.Invoices.Commands.PayInvoice;
using SubscriptionBillingPortal.Application.Features.Invoices.Queries.GetInvoices;
using SubscriptionBillingPortal.Application.Features.Subscriptions.Commands.ActivateSubscription;
using SubscriptionBillingPortal.Application.Features.Subscriptions.Commands.CancelSubscription;
using SubscriptionBillingPortal.Application.Features.Subscriptions.Commands.CreateSubscription;

namespace SubscriptionBillingPortal.Application.Tests.Validation;

public sealed class CommandValidatorTests
{
    [Fact]
    public void CreateCustomerCommandValidator_WithValidData_ShouldPassValidation()
    {
        var validator = new CreateCustomerCommandValidator();
        var result = validator.TestValidate(new CreateCustomerCommand("Jane", "Doe", "jane@example.com", Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateCustomerCommandValidator_WithEmptyFirstName_ShouldFailValidation()
    {
        var validator = new CreateCustomerCommandValidator();
        var result = validator.TestValidate(new CreateCustomerCommand(string.Empty, "Doe", "jane@example.com", Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(c => c.FirstName);
    }

    [Fact]
    public void CreateCustomerCommandValidator_WithEmptyLastName_ShouldFailValidation()
    {
        var validator = new CreateCustomerCommandValidator();
        var result = validator.TestValidate(new CreateCustomerCommand("Jane", string.Empty, "jane@example.com", Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(c => c.LastName);
    }

    [Fact]
    public void CreateCustomerCommandValidator_WithInvalidEmail_ShouldFailValidation()
    {
        var validator = new CreateCustomerCommandValidator();
        var result = validator.TestValidate(new CreateCustomerCommand("Jane", "Doe", "not-an-email", Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void CreateCustomerCommandValidator_WithEmptyIdempotencyKey_ShouldFailValidation()
    {
        var validator = new CreateCustomerCommandValidator();
        var result = validator.TestValidate(new CreateCustomerCommand("Jane", "Doe", "jane@example.com", Guid.Empty));
        result.ShouldHaveValidationErrorFor(c => c.IdempotencyKey);
    }

    [Fact]
    public void CreateSubscriptionCommandValidator_WithValidData_ShouldPassValidation()
    {
        var validator = new CreateSubscriptionCommandValidator();
        var result = validator.TestValidate(new CreateSubscriptionCommand(Guid.NewGuid(), "Pro", "Monthly", Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateSubscriptionCommandValidator_WithEmptyCustomerId_ShouldFailValidation()
    {
        var validator = new CreateSubscriptionCommandValidator();
        var result = validator.TestValidate(new CreateSubscriptionCommand(Guid.Empty, "Pro", "Monthly", Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(c => c.CustomerId);
    }

    [Fact]
    public void CreateSubscriptionCommandValidator_WithInvalidPlanType_ShouldFailValidation()
    {
        var validator = new CreateSubscriptionCommandValidator();
        var result = validator.TestValidate(new CreateSubscriptionCommand(Guid.NewGuid(), "Platinum", "Monthly", Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(c => c.PlanType);
    }

    [Fact]
    public void CreateSubscriptionCommandValidator_WithInvalidBillingInterval_ShouldFailValidation()
    {
        var validator = new CreateSubscriptionCommandValidator();
        var result = validator.TestValidate(new CreateSubscriptionCommand(Guid.NewGuid(), "Pro", "Weekly", Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(c => c.BillingInterval);
    }

    [Fact]
    public void ActivateSubscriptionCommandValidator_WithValidData_ShouldPassValidation()
    {
        var validator = new ActivateSubscriptionCommandValidator();
        var result = validator.TestValidate(new ActivateSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ActivateSubscriptionCommandValidator_WithEmptySubscriptionId_ShouldFailValidation()
    {
        var validator = new ActivateSubscriptionCommandValidator();
        var result = validator.TestValidate(new ActivateSubscriptionCommand(Guid.Empty, Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(c => c.SubscriptionId);
    }

    [Fact]
    public void CancelSubscriptionCommandValidator_WithValidData_ShouldPassValidation()
    {
        var validator = new CancelSubscriptionCommandValidator();
        var result = validator.TestValidate(new CancelSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void PayInvoiceCommandValidator_WithValidData_ShouldPassValidation()
    {
        var validator = new PayInvoiceCommandValidator();
        var result = validator.TestValidate(new PayInvoiceCommand(Guid.NewGuid(), Guid.NewGuid(), "ref-001", Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void PayInvoiceCommandValidator_WithEmptyInvoiceId_ShouldFailValidation()
    {
        var validator = new PayInvoiceCommandValidator();
        var result = validator.TestValidate(new PayInvoiceCommand(Guid.Empty, Guid.NewGuid(), "ref-001", Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(c => c.InvoiceId);
    }

    [Fact]
    public void PayInvoiceCommandValidator_WithEmptySubscriptionId_ShouldFailValidation()
    {
        var validator = new PayInvoiceCommandValidator();
        var result = validator.TestValidate(new PayInvoiceCommand(Guid.NewGuid(), Guid.Empty, "ref-001", Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(c => c.SubscriptionId);
    }

    [Fact]
    public void PayInvoiceCommandValidator_WithEmptyPaymentReference_ShouldFailValidation()
    {
        var validator = new PayInvoiceCommandValidator();
        var result = validator.TestValidate(new PayInvoiceCommand(Guid.NewGuid(), Guid.NewGuid(), string.Empty, Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(c => c.PaymentReference);
    }

    [Fact]
    public void GetInvoicesQueryValidator_WithValidData_ShouldPassValidation()
    {
        var validator = new GetInvoicesQueryValidator();
        var result = validator.TestValidate(new GetInvoicesQuery(Guid.NewGuid(), PageNumber: 1, PageSize: 20));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void GetInvoicesQueryValidator_WithEmptySubscriptionId_ShouldFailValidation()
    {
        var validator = new GetInvoicesQueryValidator();
        var result = validator.TestValidate(new GetInvoicesQuery(Guid.Empty, PageNumber: 1, PageSize: 20));
        result.ShouldHaveValidationErrorFor(q => q.SubscriptionId);
    }

    [Fact]
    public void GetInvoicesQueryValidator_WithZeroPageNumber_ShouldFailValidation()
    {
        var validator = new GetInvoicesQueryValidator();
        var result = validator.TestValidate(new GetInvoicesQuery(Guid.NewGuid(), PageNumber: 0, PageSize: 20));
        result.ShouldHaveValidationErrorFor(q => q.PageNumber);
    }

    [Fact]
    public void GetInvoicesQueryValidator_WithPageSizeOverLimit_ShouldFailValidation()
    {
        var validator = new GetInvoicesQueryValidator();
        var result = validator.TestValidate(new GetInvoicesQuery(Guid.NewGuid(), PageNumber: 1, PageSize: 101));
        result.ShouldHaveValidationErrorFor(q => q.PageSize);
    }

    [Fact]
    public void GetInvoicesQueryValidator_WithZeroPageSize_ShouldFailValidation()
    {
        var validator = new GetInvoicesQueryValidator();
        var result = validator.TestValidate(new GetInvoicesQuery(Guid.NewGuid(), PageNumber: 1, PageSize: 0));
        result.ShouldHaveValidationErrorFor(q => q.PageSize);
    }
}
