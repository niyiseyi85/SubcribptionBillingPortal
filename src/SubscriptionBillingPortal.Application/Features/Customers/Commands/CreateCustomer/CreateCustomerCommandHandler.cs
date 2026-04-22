using MediatR;
using Microsoft.Extensions.Logging;
using Mapster;
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
                "Duplicate CreateCustomerCommand detected for idempotency key '{IdempotencyKey}' — skipping.",
                command.IdempotencyKey);

            throw new InvalidOperationException(
                $"Command with idempotency key '{command.IdempotencyKey}' has already been processed.");
        }

        var customer = Customer.Create(command.FirstName, command.LastName, command.Email);

        await _unitOfWork.Customers.AddAsync(customer, cancellationToken);
        await _idempotencyService.MarkAsProcessedAsync(command.IdempotencyKey, nameof(CreateCustomerCommand), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Customer '{CustomerId}' created successfully for email '{Email}'",
            customer.Id,
            customer.Email);

        return customer.Adapt<CustomerDto>();
    }
}
