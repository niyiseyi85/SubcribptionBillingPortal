using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SubscriptionBillingPortal.IntegrationTests.Infrastructure;

namespace SubscriptionBillingPortal.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for the Invoice endpoints.
/// Covers the full subscription activation → invoice generation → payment flow.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class InvoiceEndpointTests
{
    private readonly HttpClient _client;

    public InvoiceEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(Guid SubscriptionId, Guid InvoiceId)> BootstrappedActivatedSubscriptionAsync()
    {
        // Create customer
        var customerResp = await _client.PostAsJsonAsync("/customers", new
        {
            firstName = "Invoice",
            lastName = "Tester",
            email = $"inv_{Guid.NewGuid()}@example.com"
        });
        customerResp.EnsureSuccessStatusCode();
        var customerId = (await customerResp.Content.ReadFromJsonAsync<Envelope<IdPayload>>())!.Data!.Id;

        // Create subscription
        var subResp = await _client.PostAsJsonAsync("/subscriptions", new
        {
            customerId,
            planType = "Pro",
            billingInterval = "Monthly"
        });
        subResp.EnsureSuccessStatusCode();
        var subscriptionId = (await subResp.Content.ReadFromJsonAsync<Envelope<IdPayload>>())!.Data!.Id;

        // Activate — generates invoice #1
        (await _client.PostAsync($"/subscriptions/{subscriptionId}/activate", null)).EnsureSuccessStatusCode();

        // Fetch the invoice id
        var invoicesResp = await _client.GetAsync($"/invoices?subscriptionId={subscriptionId}&pageNumber=1&pageSize=10");
        invoicesResp.EnsureSuccessStatusCode();
        var invoices = await invoicesResp.Content.ReadFromJsonAsync<Envelope<PagedPayload<InvoicePayload>>>();
        var invoiceId = invoices!.Data!.Items.First().Id;

        return (subscriptionId, invoiceId);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetInvoices_AfterActivation_ShouldReturnOneInvoice()
    {
        var (subscriptionId, _) = await BootstrappedActivatedSubscriptionAsync();

        var response = await _client.GetAsync(
            $"/invoices?subscriptionId={subscriptionId}&pageNumber=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Envelope<PagedPayload<InvoicePayload>>>();
        body!.Data!.TotalCount.Should().Be(1);
        body.Data.Items.Should().HaveCount(1);
        body.Data.Items.First().Status.Should().Be("Pending");
    }

    [Fact]
    public async Task PayInvoice_WithValidPendingInvoice_ShouldReturn200AndPaidStatus()
    {
        var (subscriptionId, invoiceId) = await BootstrappedActivatedSubscriptionAsync();

        var response = await _client.PostAsJsonAsync($"/invoices/{invoiceId}/pay", new
        {
            subscriptionId,
            paymentReference = "ref-integration-001"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Envelope<InvoicePayload>>();
        body!.Data!.Status.Should().Be("Paid");
        body.Data.PaymentReference.Should().Be("ref-integration-001");
    }

    [Fact]
    public async Task PayInvoice_WithSameReferenceOnAlreadyPaidInvoice_ShouldReturn200Idempotent()
    {
        var (subscriptionId, invoiceId) = await BootstrappedActivatedSubscriptionAsync();

        // First payment
        (await _client.PostAsJsonAsync($"/invoices/{invoiceId}/pay", new
        {
            subscriptionId,
            paymentReference = "ref-idempotent-001"
        })).EnsureSuccessStatusCode();

        // Retry with the same reference — idempotent, must succeed
        var retry = await _client.PostAsJsonAsync($"/invoices/{invoiceId}/pay", new
        {
            subscriptionId,
            paymentReference = "ref-idempotent-001"
        });

        retry.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PayInvoice_WithDifferentReferenceOnAlreadyPaidInvoice_ShouldReturn422()
    {
        var (subscriptionId, invoiceId) = await BootstrappedActivatedSubscriptionAsync();

        (await _client.PostAsJsonAsync($"/invoices/{invoiceId}/pay", new
        {
            subscriptionId,
            paymentReference = "ref-first-001"
        })).EnsureSuccessStatusCode();

        // Different reference on a paid invoice — must be rejected
        var response = await _client.PostAsJsonAsync($"/invoices/{invoiceId}/pay", new
        {
            subscriptionId,
            paymentReference = "ref-different-002"
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── Envelope helpers ──────────────────────────────────────────────────────

    private sealed class Envelope<T>
    {
        public bool Success { get; init; }
        public T? Data { get; init; }
    }

    private sealed class IdPayload
    {
        public Guid Id { get; init; }
    }

    private sealed class PagedPayload<T>
    {
        public IReadOnlyList<T> Items { get; init; } = [];
        public int TotalCount { get; init; }
    }

    private sealed class InvoicePayload
    {
        public Guid Id { get; init; }
        public string Status { get; init; } = string.Empty;
        public string? PaymentReference { get; init; }
        public decimal Amount { get; init; }
    }
}
