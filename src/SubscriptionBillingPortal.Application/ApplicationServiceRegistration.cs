using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionBillingPortal.Application.Behaviours;
using SubscriptionBillingPortal.Application.Mappings;

namespace SubscriptionBillingPortal.Application;

/// <summary>
/// Registers all Application layer services into the DI container.
/// Pipeline order: Logging → Validation → Handler
/// </summary>
public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        MappingConfiguration.Configure();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceRegistration).Assembly);
        });

        services.AddValidatorsFromAssembly(typeof(ApplicationServiceRegistration).Assembly);

        // Pipeline order matters — behaviours execute in registration order
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingPipelineBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehaviour<,>));

        return services;
    }
}
