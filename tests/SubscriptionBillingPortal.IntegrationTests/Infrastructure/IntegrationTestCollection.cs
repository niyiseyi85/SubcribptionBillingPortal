namespace SubscriptionBillingPortal.IntegrationTests.Infrastructure;

/// <summary>
/// Placing all integration-test classes in the same xUnit collection serialises
/// their execution.  This prevents multiple <see cref="CustomWebApplicationFactory"/>
/// instances (and their background services) from running concurrently, which can
/// interfere with the shared static Serilog bootstrap logger and with EF Core's
/// internal service-provider cache.
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "Integration";
}
