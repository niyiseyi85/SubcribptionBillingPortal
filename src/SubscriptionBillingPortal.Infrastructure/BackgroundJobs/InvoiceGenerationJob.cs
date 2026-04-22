using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SubscriptionBillingPortal.Application.Contracts.Persistence;

namespace SubscriptionBillingPortal.Infrastructure.BackgroundJobs;

/// <summary>
/// Background billing job — periodically generates invoices for subscriptions
/// that are due for billing (Status == Active AND NextBillingDate &lt;= UtcNow).
///
/// The domain's billing cycle guard (NextBillingDate on the Subscription aggregate)
/// ensures duplicate invoices are impossible even if the job runs more frequently.
///
/// Domain events raised by GenerateInvoice() are written to the Outbox atomically
/// inside UnitOfWork.SaveChangesAsync() — no manual event dispatching needed here.
/// </summary>
public sealed class InvoiceGenerationJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InvoiceGenerationJob> _logger;
    private readonly TimeSpan _executionInterval;

    public InvoiceGenerationJob(
        IServiceScopeFactory scopeFactory,
        ILogger<InvoiceGenerationJob> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var intervalValue = configuration["BackgroundJobs:InvoiceGenerationInterval"];
        _executionInterval = intervalValue is not null && TimeSpan.TryParse(intervalValue, out var parsed)
            ? parsed
            : TimeSpan.FromMinutes(1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("InvoiceGenerationJob started. Running every {Interval}m.", _executionInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunBillingCycleAsync(stoppingToken);
            await Task.Delay(_executionInterval, stoppingToken);
        }

        _logger.LogInformation("InvoiceGenerationJob stopped.");
    }

    /// <summary>
    /// Executes a single billing cycle immediately.
    /// Exposed as internal for integration tests — avoids the polling loop overhead.
    /// </summary>
    internal Task RunOnceAsync(CancellationToken cancellationToken) =>
        RunBillingCycleAsync(cancellationToken);

    private async Task RunBillingCycleAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("InvoiceGenerationJob: Starting billing cycle at {UtcNow:O}.", DateTimeOffset.UtcNow);

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var asOf = DateTimeOffset.UtcNow;

        // Only fetch subscriptions that are Active AND whose NextBillingDate is due
        var dueSubscriptions = await unitOfWork.Subscriptions.GetAllDueForBillingAsync(asOf, cancellationToken);

        if (dueSubscriptions.Count == 0)
        {
            _logger.LogInformation("InvoiceGenerationJob: No subscriptions due for billing.");
            return;
        }

        _logger.LogInformation(
            "InvoiceGenerationJob: {Count} subscription(s) due for billing.",
            dueSubscriptions.Count);

        var generated = 0;
        var failed = 0;

        foreach (var subscription in dueSubscriptions)
        {
            try
            {
                subscription.GenerateInvoice();
                generated++;

                _logger.LogInformation(
                    "InvoiceGenerationJob: Invoice generated for subscription '{SubscriptionId}'. Next billing at {NextBillingDate:O}.",
                    subscription.Id,
                    subscription.NextBillingDate);
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(
                    ex,
                    "InvoiceGenerationJob: Failed to generate invoice for subscription '{SubscriptionId}'.",
                    subscription.Id);
            }
        }

        // Domain events from GenerateInvoice() are captured and written to the Outbox atomically here
        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "InvoiceGenerationJob: Billing cycle complete. Generated: {Generated}, Failed: {Failed}.",
            generated,
            failed);
    }
}

