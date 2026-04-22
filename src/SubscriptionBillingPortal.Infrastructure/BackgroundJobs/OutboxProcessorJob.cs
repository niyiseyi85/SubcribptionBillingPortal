using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SubscriptionBillingPortal.Domain.Common;
using SubscriptionBillingPortal.Infrastructure.Persistence;
using SubscriptionBillingPortal.Infrastructure.Services;

namespace SubscriptionBillingPortal.Infrastructure.BackgroundJobs;

/// <summary>
/// Outbox pattern processor — polls for unprocessed OutboxMessages, deserializes
/// each domain event from its JSON payload, publishes it via MediatR, and marks it
/// as processed. Guarantees at-least-once delivery: if the process crashes before
/// marking a message processed, it will be retried on the next poll.
/// </summary>
public sealed class OutboxProcessorJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorJob> _logger;
    private readonly TimeSpan _pollingInterval;

    public OutboxProcessorJob(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessorJob> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var intervalValue = configuration["BackgroundJobs:OutboxPollingInterval"];
        _pollingInterval = intervalValue is not null && TimeSpan.TryParse(intervalValue, out var parsed)
            ? parsed
            : TimeSpan.FromSeconds(15);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessorJob started. Polling every {Interval}s.", _pollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessOutboxMessagesAsync(stoppingToken);
            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("OutboxProcessorJob stopped.");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var unprocessedMessages = await context.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (unprocessedMessages.Count == 0)
        {
            return;
        }

        _logger.LogInformation(
            "OutboxProcessorJob: Processing {Count} unprocessed outbox message(s).",
            unprocessedMessages.Count);

        foreach (var message in unprocessedMessages)
        {
            _logger.LogInformation(
                "Processing OutboxMessage '{MessageId}' of type '{EventType}'",
                message.Id,
                message.EventType);

            try
            {
                var eventType = Type.GetType(message.EventType);

                if (eventType is null)
                {
                    _logger.LogWarning(
                        "OutboxProcessorJob: Could not resolve CLR type '{EventType}' for message '{MessageId}'. Skipping.",
                        message.EventType,
                        message.Id);

                    message.MarkAsProcessed();
                    continue;
                }

                var domainEvent = (IDomainEvent)JsonSerializer.Deserialize(message.Payload, eventType)!;

                // Wrap in a MediatR notification and publish — handlers registered via
                // INotificationHandler<DomainEventNotification> will receive it
                var notification = new DomainEventNotification(domainEvent);
                await publisher.Publish(notification, cancellationToken);

                message.MarkAsProcessed();

                _logger.LogInformation(
                    "Successfully processed event '{EventType}' from OutboxMessage '{MessageId}'",
                    eventType.Name,
                    message.Id);
            }
            catch (Exception ex)
            {
                // Do NOT mark as processed — message will be retried on the next poll cycle
                _logger.LogError(
                    ex,
                    "OutboxProcessorJob: Failed to process OutboxMessage '{MessageId}' of type '{EventType}'. Will retry.",
                    message.Id,
                    message.EventType);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}

