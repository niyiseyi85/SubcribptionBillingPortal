using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SubscriptionBillingPortal.IntegrationTests.Infrastructure;

namespace SubscriptionBillingPortal.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for the Customer endpoints.
/// Uses a real HTTP client against the full ASP.NET Core pipeline with InMemory EF.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class CustomerEndpointTests
{
    private readonly HttpClient _client;

    public CustomerEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateCustomer_WithValidRequest_ShouldReturn201()
    {
        var request = new
        {
            firstName = "Jane",
            lastName = "Doe",
            email = "jane@example.com"
        };

        var response = await _client.PostAsJsonAsync("/customers", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateCustomer_WithValidRequest_ShouldReturnCustomerInBody()
    {
        var request = new
        {
            firstName = "John",
            lastName = "Smith",
            email = "john.smith@example.com"
        };

        var response = await _client.PostAsJsonAsync("/customers", request);
        var body = await response.Content.ReadFromJsonAsync<ApiResponseEnvelope<CustomerPayload>>();

        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.FirstName.Should().Be("John");
        body.Data.LastName.Should().Be("Smith");
        body.Data.Email.Should().Be("john.smith@example.com");
    }

    [Fact]
    public async Task CreateCustomer_WithInvalidEmail_ShouldReturn400()
    {
        var request = new { firstName = "Jane", lastName = "Doe", email = "not-an-email" };

        var response = await _client.PostAsJsonAsync("/customers", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCustomer_WithEmptyFirstName_ShouldReturn400()
    {
        var request = new { firstName = "", lastName = "Doe", email = "jane@example.com" };

        var response = await _client.PostAsJsonAsync("/customers", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Shared response envelope helpers (used across endpoint test classes) ──

    internal sealed class ApiResponseEnvelope<T>
    {
        public bool Success { get; init; }
        public int StatusCode { get; init; }
        public string? Message { get; init; }
        public T? Data { get; init; }
    }

    internal sealed class CustomerPayload
    {
        public Guid Id { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
    }
}
