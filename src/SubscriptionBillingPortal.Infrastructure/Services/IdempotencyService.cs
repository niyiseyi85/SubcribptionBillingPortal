using Microsoft.EntityFrameworkCore;
using SubscriptionBillingPortal.Application.Contracts.Services;
using SubscriptionBillingPortal.Domain.Common;
using SubscriptionBillingPortal.Infrastructure.Persistence;

namespace SubscriptionBillingPortal.Infrastructure.Services;

/// <summary>
/// Idempotency service backed by the database.
/// Prevents duplicate command processing by checking persisted idempotency keys.
/// </summary>
public sealed class IdempotencyService : IIdempotencyService
{
    private readonly ApplicationDbContext _context;

    public IdempotencyService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasBeenProcessedAsync(Guid idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.IdempotencyRecords
            .AnyAsync(r => r.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<string?> GetResponseAsync(Guid idempotencyKey, CancellationToken cancellationToken = default)
    {
        var record = await _context.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey, cancellationToken);
        return record?.ResponseJson;
    }

    public async Task MarkAsProcessedAsync(Guid idempotencyKey, string commandName, string responseJson, CancellationToken cancellationToken = default)
    {
        var record = IdempotencyRecord.Create(idempotencyKey, commandName, responseJson);
        await _context.IdempotencyRecords.AddAsync(record, cancellationToken);
    }
}
