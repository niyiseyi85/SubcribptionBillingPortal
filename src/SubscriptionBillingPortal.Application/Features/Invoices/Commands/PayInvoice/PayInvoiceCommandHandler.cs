using MediatR;
using Microsoft.Extensions.Logging;
using Mapster;
using System.Text.Json;
using SubscriptionBillingPortal.Application.Contracts.Persistence;
using SubscriptionBillingPortal.Application.Contracts.Services;
using SubscriptionBillingPortal.Application.DTOs;

namespace SubscriptionBillingPortal.Application.Features.Invoices.Commands.PayInvoice;

public sealed class PayInvoiceCommandHandler : IRequestHandler<PayInvoiceCommand, InvoiceDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<PayInvoiceCommandHandler> _logger;

    public PayInvoiceCommandHandler(
        IUnitOfWork unitOfWork,
        IIdempotencyService idempotencyService,
        ILogger<PayInvoiceCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task<InvoiceDto> Handle(PayInvoiceCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing payment for invoice '{InvoiceId}' on subscription '{SubscriptionId}' with reference '{PaymentReference}'",
            command.InvoiceId,
            command.SubscriptionId,
            command.PaymentReference);

        if (await _idempotencyService.HasBeenProcessedAsync(command.IdempotencyKey, cancellationToken))
        {
            _logger.LogWarning(
                "Duplicate PayInvoiceCommand detected for idempotency key '{IdempotencyKey}' — returning cached response.",
                command.IdempotencyKey);

            var cachedJson = await _idempotencyService.GetResponseAsync(command.IdempotencyKey, cancellationToken)
                ?? throw new InvalidOperationException($"Idempotency record exists for key '{command.IdempotencyKey}' but contains no cached response.");
            return JsonSerializer.Deserialize<InvoiceDto>(cachedJson)!;
        }

        var subscription = await _unitOfWork.Subscriptions.GetByIdAsync(command.SubscriptionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Subscription '{command.SubscriptionId}' was not found.");

        // Domain enforces idempotency: same PaymentReference on a paid invoice is a safe no-op
        subscription.PayInvoice(command.InvoiceId, command.PaymentReference);

        var invoice = subscription.Invoices.First(i => i.Id == command.InvoiceId);
        var dto = invoice.Adapt<InvoiceDto>();

        await _idempotencyService.MarkAsProcessedAsync(command.IdempotencyKey, nameof(PayInvoiceCommand), JsonSerializer.Serialize(dto), cancellationToken);

        // UnitOfWork.SaveChangesAsync captures domain events and writes them to the Outbox atomically
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Invoice '{InvoiceId}' paid successfully on subscription '{SubscriptionId}'",
            invoice.Id,
            subscription.Id);

        return invoice.Adapt<InvoiceDto>();
    }
}
