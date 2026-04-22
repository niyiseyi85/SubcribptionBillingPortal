using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly CustomWebApplicationFactory _factory;

    public InvoiceEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
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

    [Fact]
    public async Task GetInvoices_WithUnknownSubscriptionId_ShouldReturn404()
    {
        var response = await _client.GetAsync(
            $"/invoices?subscriptionId={Guid.NewGuid()}&pageNumber=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PayInvoice_WithUnknownSubscriptionId_ShouldReturn404()
    {
        var response = await _client.PostAsJsonAsync($"/invoices/{Guid.NewGuid()}/pay", new
        {
            subscriptionId = Guid.NewGuid(),
            paymentReference = "ref-001"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PayInvoice_WithEmptyPaymentReference_ShouldReturn400()
    {
        var (subscriptionId, invoiceId) = await BootstrappedActivatedSubscriptionAsync();

        var response = await _client.PostAsJsonAsync($"/invoices/{invoiceId}/pay", new
        {
            subscriptionId,
            paymentReference = string.Empty
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetInvoices_WithMultipleInvoices_ShouldReturnCorrectPaginatedData()
    {
        // Arrange — create a subscription, activate it (invoice #1), then generate 2 more via the job
        var customerResp = await _client.PostAsJsonAsync("/customers", new
        {
            firstName = "Page",
            lastName = "Tester",
            email = $"page_{Guid.NewGuid()}@example.com"
        });
        customerResp.EnsureSuccessStatusCode();
        var customerId = (await customerResp.Content.ReadFromJsonAsync<Envelope<IdPayload>>())!.Data!.Id;

        var subResp = await _client.PostAsJsonAsync("/subscriptions", new
        {
            customerId,
            planType = "Basic",
            billingInterval = "Monthly"
        });
        subResp.EnsureSuccessStatusCode();
        var subscriptionId = (await subResp.Content.ReadFromJsonAsync<Envelope<IdPayload>>())!.Data!.Id;
        (await _client.PostAsync($"/subscriptions/{subscriptionId}/activate", null)).EnsureSuccessStatusCode();

        // Seed 2 extra invoices by running the job with a backdated billing date
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubscriptionBillingPortal.Infrastructure.Persistence.ApplicationDbContext>();
        var tracked = await db.Set<SubscriptionBillingPortal.Domain.Aggregates.Subscription>().FindAsync(subscriptionId);
        db.Entry(tracked!).Property("NextBillingDate").CurrentValue = DateTimeOffset.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();

        var job = new SubscriptionBillingPortal.Infrastructure.BackgroundJobs.InvoiceGenerationJob(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SubscriptionBillingPortal.Infrastructure.BackgroundJobs.InvoiceGenerationJob>.Instance,
            _factory.Services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>());
        await job.RunOnceAsync(CancellationToken.None);

        // Re-backdate and run again for invoice #3
        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<SubscriptionBillingPortal.Infrastructure.Persistence.ApplicationDbContext>();
        var tracked2 = await db2.Set<SubscriptionBillingPortal.Domain.Aggregates.Subscription>().FindAsync(subscriptionId);
        db2.Entry(tracked2!).Property("NextBillingDate").CurrentValue = DateTimeOffset.UtcNow.AddDays(-1);
        await db2.SaveChangesAsync();
        await job.RunOnceAsync(CancellationToken.None);

        // Act — request page 1 with pageSize=2
        var page1 = await _client.GetAsync(
            $"/invoices?subscriptionId={subscriptionId}&pageNumber=1&pageSize=2");
        page1.StatusCode.Should().Be(HttpStatusCode.OK);
        var body1 = await page1.Content.ReadFromJsonAsync<Envelope<PagedPayload<InvoicePayload>>>();
        body1!.Data!.TotalCount.Should().Be(3);
        body1.Data.Items.Should().HaveCount(2);

        // Act — request page 2
        var page2 = await _client.GetAsync(
            $"/invoices?subscriptionId={subscriptionId}&pageNumber=2&pageSize=2");
        page2.StatusCode.Should().Be(HttpStatusCode.OK);
        var body2 = await page2.Content.ReadFromJsonAsync<Envelope<PagedPayload<InvoicePayload>>>();
        body2!.Data!.Items.Should().HaveCount(1);
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
