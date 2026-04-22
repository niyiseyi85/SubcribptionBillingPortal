using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionBillingPortal.Application.Contracts.Persistence;
using SubscriptionBillingPortal.Application.Contracts.Services;
using SubscriptionBillingPortal.Infrastructure.BackgroundJobs;
using SubscriptionBillingPortal.Infrastructure.Persistence;
using SubscriptionBillingPortal.Infrastructure.Services;

namespace SubscriptionBillingPortal.Infrastructure;

/// <summary>
/// Registers all Infrastructure layer services into the DI container.
/// </summary>
public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseInMemoryDatabase("SubscriptionBillingPortalDb");
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIdempotencyService, IdempotencyService>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddHostedService<OutboxProcessorJob>();
        services.AddHostedService<InvoiceGenerationJob>();

        return services;
    }
}
