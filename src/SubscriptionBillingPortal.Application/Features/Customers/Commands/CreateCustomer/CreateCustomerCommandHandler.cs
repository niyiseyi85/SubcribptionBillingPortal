using MediatR;
using Microsoft.Extensions.Logging;
using Mapster;
using System.Text.Json;
using SubscriptionBillingPortal.Application.Contracts.Persistence;
using SubscriptionBillingPortal.Application.Contracts.Services;
using SubscriptionBillingPortal.Application.DTOs;
using SubscriptionBillingPortal.Domain.Aggregates;

namespace SubscriptionBillingPortal.Application.Features.Customers.Commands.CreateCustomer;

public sealed class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<CreateCustomerCommandHandler> _logger;

    public CreateCustomerCommandHandler(
        IUnitOfWork unitOfWork,
        IIdempotencyService idempotencyService,
        ILogger<CreateCustomerCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task<CustomerDto> Handle(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling CreateCustomerCommand for email '{Email}' with idempotency key '{IdempotencyKey}'",
            command.Email,
            command.IdempotencyKey);

        if (await _idempotencyService.HasBeenProcessedAsync(command.IdempotencyKey, cancellationToken))
        {
            _logger.LogWarning(
                "Duplicate CreateCustomerCommand detected for idempotency key '{IdempotencyKey}' — returning cached response.",
                command.IdempotencyKey);

            var cachedJson = await _idempotencyService.GetResponseAsync(command.IdempotencyKey, cancellationToken)
                ?? throw new InvalidOperationException($"Idempotency record exists for key '{command.IdempotencyKey}' but contains no cached response.");
            return JsonSerializer.Deserialize<CustomerDto>(cachedJson)!;
        }

        var customer = Customer.Create(command.FirstName, command.LastName, command.Email);

        await _unitOfWork.Customers.AddAsync(customer, cancellationToken);
        var dto = customer.Adapt<CustomerDto>();
        await _idempotencyService.MarkAsProcessedAsync(command.IdempotencyKey, nameof(CreateCustomerCommand), JsonSerializer.Serialize(dto), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Customer '{CustomerId}' created successfully for email '{Email}'",
            customer.Id,
            customer.Email);

        return dto;
    }
}
