using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SubscriptionBillingPortal.Infrastructure.Persistence;

namespace SubscriptionBillingPortal.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real API pipeline with an isolated SQLite in-memory database.
/// Each factory instance opens one named shared-cache connection that keeps the
/// database alive for the lifetime of the fixture, so every request scope sees
/// the same data.  SQLite in-memory avoids the EF Core InMemory provider's
/// limitations with OwnsOne update tracking and ComplexProperty query compilation.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"IntegrationTestDb_{Guid.NewGuid()}";

    /// <summary>
    /// Sentinel connection that keeps the named in-memory SQLite database alive.
    /// SQLite drops an in-memory database when the last connection to it closes,
    /// so we hold one open for the entire lifetime of this factory.
    /// </summary>
    private SqliteConnection? _keepAliveConnection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Open the sentinel connection so the named in-memory DB persists.
            _keepAliveConnection = new SqliteConnection(ConnectionString);
            _keepAliveConnection.Open();

            // Remove every registration that ties the context to the production DB.
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                         || d.ServiceType == typeof(IDbContextOptionsConfiguration<ApplicationDbContext>))
                .ToList();

            foreach (var descriptor in toRemove)
                services.Remove(descriptor);

            // Register with the factory-scoped SQLite in-memory database.
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(ConnectionString));

            // Prevent a crashing BackgroundService from tearing down the entire
            // test host before the schema is created or tests have finished.
            services.Configure<HostOptions>(opts =>
                opts.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Create the SQLite schema once, after the host is built.
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keepAliveConnection?.Close();
            _keepAliveConnection?.Dispose();
        }
    }

    private string ConnectionString =>
        $"Data Source={_dbName};Mode=Memory;Cache=Shared";
}

