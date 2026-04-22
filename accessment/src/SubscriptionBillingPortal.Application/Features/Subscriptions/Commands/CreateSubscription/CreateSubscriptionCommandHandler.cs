using MediatR;
using Microsoft.Extensions.Logging;
using Mapster;
using SubscriptionBillingPortal.Application.Contracts.Persistence;
using SubscriptionBillingPortal.Application.Contracts.Services;
using SubscriptionBillingPortal.Application.DTOs;
using SubscriptionBillingPortal.Domain.Aggregates;
using SubscriptionBillingPortal.Domain.Enums;
using SubscriptionBillingPortal.Domain.ValueObjects;

namespace SubscriptionBillingPortal.Application.Features.Subscriptions.Commands.CreateSubscription;

public sealed class CreateSubscriptionCommandHandler : IRequestHandler<CreateSubscriptionCommand, SubscriptionDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<CreateSubscriptionCommandHandler> _logger;

    public CreateSubscriptionCommandHandler(
        IUnitOfWork unitOfWork,
        IIdempotencyService idempotencyService,
        ILogger<CreateSubscriptionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task<SubscriptionDto> Handle(CreateSubscriptionCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling CreateSubscriptionCommand for customer '{CustomerId}' with idempotency key '{IdempotencyKey}'",
            command.CustomerId,
            command.IdempotencyKey);

        if (await _idempotencyService.HasBeenProcessedAsync(command.IdempotencyKey, cancellationToken))
        {
            _logger.LogWarning(
                "Duplicate CreateSubscriptionCommand detected for idempotency key '{IdempotencyKey}' — skipping.",
                command.IdempotencyKey);

            throw new InvalidOperationException(
                $"Command with idempotency key '{command.IdempotencyKey}' has already been processed.");
        }

        var customerExists = await _unitOfWork.Customers.ExistsAsync(command.CustomerId, cancellationToken);
        if (!customerExists)
        {
            throw new KeyNotFoundException($"Customer '{command.CustomerId}' was not found.");
        }

        var planType = Enum.Parse<PlanType>(command.PlanType, ignoreCase: true);
        var billingInterval = Enum.Parse<BillingInterval>(command.BillingInterval, ignoreCase: true);
        var plan = SubscriptionPlan.Create(planType, billingInterval);

        var subscription = Subscription.Create(command.CustomerId, plan);

        await _unitOfWork.Subscriptions.AddAsync(subscription, cancellationToken);
        await _idempotencyService.MarkAsProcessedAsync(command.IdempotencyKey, nameof(CreateSubscriptionCommand), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Subscription '{SubscriptionId}' created for customer '{CustomerId}'",
            subscription.Id,
            subscription.CustomerId);

        return subscription.Adapt<SubscriptionDto>();
    }
}
