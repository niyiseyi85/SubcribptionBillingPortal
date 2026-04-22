using Microsoft.EntityFrameworkCore;
using SubscriptionBillingPortal.Application.Contracts.Persistence;
using SubscriptionBillingPortal.Domain.Aggregates;
using SubscriptionBillingPortal.Infrastructure.Persistence;

namespace SubscriptionBillingPortal.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of ICustomerRepository.
/// No business logic — delegates aggregate handling to the domain.
/// </summary>
public sealed class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _context;

    public CustomerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await _context.Customers.AddAsync(customer, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .AnyAsync(c => c.Id == customerId, cancellationToken);
    }
}
