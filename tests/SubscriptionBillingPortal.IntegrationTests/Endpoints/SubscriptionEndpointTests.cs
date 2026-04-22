using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SubscriptionBillingPortal.IntegrationTests.Infrastructure;

namespace SubscriptionBillingPortal.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for Subscription endpoints.
/// Covers the full create → activate → cancel lifecycle through the HTTP pipeline.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class SubscriptionEndpointTests
{
    private readonly HttpClient _client;

    public SubscriptionEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Guid> CreateCustomerAsync()
    {
        var response = await _client.PostAsJsonAsync("/customers", new
        {
            firstName = "Test",
            lastName = "User",
            email = $"user_{Guid.NewGuid()}@example.com"
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<Envelope<IdPayload>>();
        return body!.Data!.Id;
    }

    private async Task<Guid> CreateSubscriptionAsync(Guid customerId, string planType = "Pro", string billingInterval = "Monthly")
    {
        var response = await _client.PostAsJsonAsync("/subscriptions", new
        {
            customerId,
            planType,
            billingInterval
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<Envelope<IdPayload>>();
        return body!.Data!.Id;
    }

    private async Task ActivateSubscriptionAsync(Guid subscriptionId)
    {
        var response = await _client.PostAsync($"/subscriptions/{subscriptionId}/activate", null);
        response.EnsureSuccessStatusCode();
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSubscription_WithValidRequest_ShouldReturn201()
    {
        var customerId = await CreateCustomerAsync();

        var response = await _client.PostAsJsonAsync("/subscriptions", new
        {
            customerId,
            planType = "Pro",
            billingInterval = "Monthly"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateSubscription_WithInvalidPlanType_ShouldReturn400()
    {
        var customerId = await CreateCustomerAsync();

        var response = await _client.PostAsJsonAsync("/subscriptions", new
        {
            customerId,
            planType = "Platinum",
            billingInterval = "Monthly"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateSubscription_WithUnknownCustomer_ShouldReturn404()
    {
        var response = await _client.PostAsJsonAsync("/subscriptions", new
        {
            customerId = Guid.NewGuid(),
            planType = "Basic",
            billingInterval = "Monthly"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ActivateSubscription_WithInactiveSubscription_ShouldReturn200()
    {
        var customerId = await CreateCustomerAsync();
        var subscriptionId = await CreateSubscriptionAsync(customerId);

        var response = await _client.PostAsync($"/subscriptions/{subscriptionId}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ActivateSubscription_TwiceOnSameSubscription_ShouldReturn422()
    {
        var customerId = await CreateCustomerAsync();
        var subscriptionId = await CreateSubscriptionAsync(customerId);
        await ActivateSubscriptionAsync(subscriptionId);

        // Second activation with a new idempotency key → business rule violation
        var response = await _client.PostAsync($"/subscriptions/{subscriptionId}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CancelSubscription_WithActiveSubscription_ShouldReturn200()
    {
        var customerId = await CreateCustomerAsync();
        var subscriptionId = await CreateSubscriptionAsync(customerId);
        await ActivateSubscriptionAsync(subscriptionId);

        var response = await _client.PostAsync($"/subscriptions/{subscriptionId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CancelSubscription_ResponseShouldContainCancelledStatus()
    {
        var customerId = await CreateCustomerAsync();
        var subscriptionId = await CreateSubscriptionAsync(customerId);
        await ActivateSubscriptionAsync(subscriptionId);

        var response = await _client.PostAsync($"/subscriptions/{subscriptionId}/cancel", null);
        var body = await response.Content.ReadFromJsonAsync<Envelope<SubscriptionPayload>>();

        body!.Data!.Status.Should().Be("Cancelled");
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

    private sealed class SubscriptionPayload
    {
        public Guid Id { get; init; }
        public string Status { get; init; } = string.Empty;
        public string PlanType { get; init; } = string.Empty;
        public string BillingInterval { get; init; } = string.Empty;
        public decimal PlanPrice { get; init; }
    }
}
