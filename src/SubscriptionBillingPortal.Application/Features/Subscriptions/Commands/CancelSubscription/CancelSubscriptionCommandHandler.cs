using MediatR;
using Microsoft.Extensions.Logging;
using Mapster;
using System.Text.Json;
using SubscriptionBillingPortal.Application.Contracts.Persistence;
using SubscriptionBillingPortal.Application.Contracts.Services;
using SubscriptionBillingPortal.Application.DTOs;

namespace SubscriptionBillingPortal.Application.Features.Subscriptions.Commands.CancelSubscription;

public sealed class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand, SubscriptionDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<CancelSubscriptionCommandHandler> _logger;

    public CancelSubscriptionCommandHandler(
        IUnitOfWork unitOfWork,
        IIdempotencyService idempotencyService,
        ILogger<CancelSubscriptionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task<SubscriptionDto> Handle(CancelSubscriptionCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Cancelling subscription '{SubscriptionId}'",
            command.SubscriptionId);

        if (await _idempotencyService.HasBeenProcessedAsync(command.IdempotencyKey, cancellationToken))
        {
            _logger.LogWarning(
                "Duplicate CancelSubscriptionCommand detected for idempotency key '{IdempotencyKey}' — returning cached response.",
                command.IdempotencyKey);

            var cachedJson = await _idempotencyService.GetResponseAsync(command.IdempotencyKey, cancellationToken)
                ?? throw new InvalidOperationException($"Idempotency record exists for key '{command.IdempotencyKey}' but contains no cached response.");
            return JsonSerializer.Deserialize<SubscriptionDto>(cachedJson)!;
        }

        var subscription = await _unitOfWork.Subscriptions.GetByIdAsync(command.SubscriptionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Subscription '{command.SubscriptionId}' was not found.");

        subscription.Cancel();

        var dto = subscription.Adapt<SubscriptionDto>();
        await _idempotencyService.MarkAsProcessedAsync(command.IdempotencyKey, nameof(CancelSubscriptionCommand), JsonSerializer.Serialize(dto), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Subscription '{SubscriptionId}' cancelled successfully",
            subscription.Id);

        return dto;
    }
}
