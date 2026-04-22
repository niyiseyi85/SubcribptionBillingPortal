using MediatR;
using Microsoft.Extensions.Logging;
using Mapster;
using SubscriptionBillingPortal.Application.Contracts.Persistence;
using SubscriptionBillingPortal.Application.Contracts.Services;
using SubscriptionBillingPortal.Application.DTOs;

namespace SubscriptionBillingPortal.Application.Features.Subscriptions.Commands.ActivateSubscription;

public sealed class ActivateSubscriptionCommandHandler : IRequestHandler<ActivateSubscriptionCommand, SubscriptionDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<ActivateSubscriptionCommandHandler> _logger;

    public ActivateSubscriptionCommandHandler(
        IUnitOfWork unitOfWork,
        IIdempotencyService idempotencyService,
        ILogger<ActivateSubscriptionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task<SubscriptionDto> Handle(ActivateSubscriptionCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Activating subscription '{SubscriptionId}'",
            command.SubscriptionId);

        if (await _idempotencyService.HasBeenProcessedAsync(command.IdempotencyKey, cancellationToken))
        {
            _logger.LogWarning(
                "Duplicate ActivateSubscriptionCommand detected for idempotency key '{IdempotencyKey}' — skipping.",
                command.IdempotencyKey);

            throw new InvalidOperationException(
                $"Command with idempotency key '{command.IdempotencyKey}' has already been processed.");
        }

        var subscription = await _unitOfWork.Subscriptions.GetByIdAsync(command.SubscriptionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Subscription '{command.SubscriptionId}' was not found.");

        subscription.Activate();

        await _idempotencyService.MarkAsProcessedAsync(command.IdempotencyKey, nameof(ActivateSubscriptionCommand), cancellationToken);

        // UnitOfWork.SaveChangesAsync captures domain events and writes them to the Outbox atomically
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Subscription '{SubscriptionId}' activated successfully",
            subscription.Id);

        return subscription.Adapt<SubscriptionDto>();
    }
}
