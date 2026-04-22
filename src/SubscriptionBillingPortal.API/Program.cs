using Scalar.AspNetCore;
using Serilog;
using SubscriptionBillingPortal.API.Endpoints;
using SubscriptionBillingPortal.API.Middleware;
using SubscriptionBillingPortal.Application;
using SubscriptionBillingPortal.Infrastructure;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // ── Service Registration ──────────────────────────────────────────────────────

    builder.Services.AddOpenApi();
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices();

    // ── Build ─────────────────────────────────────────────────────────────────────

    var app = builder.Build();

    // ── Middleware Pipeline ───────────────────────────────────────────────────────

    app.UseSerilogRequestLogging();

    // Integrates with AddProblemDetails() + AddExceptionHandler<T>() for RFC 7807 responses
    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();                   // raw spec  → /openapi/v1.json
        app.MapScalarApiReference(options =>
        {
            options.Title       = "Subscription Billing Portal";
            options.Theme       = ScalarTheme.Purple;
            options.DefaultHttpClient = new(ScalarTarget.Http, ScalarClient.HttpClient);
        });                                 // UI         → /scalar/v1
    }

    app.UseHttpsRedirection();

    // ── Endpoint Mapping ──────────────────────────────────────────────────────────

    app.MapCustomerEndpoints();
    app.MapSubscriptionEndpoints();
    app.MapInvoiceEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

// Required by WebApplicationFactory<Program> in integration tests.
public partial class Program { }
