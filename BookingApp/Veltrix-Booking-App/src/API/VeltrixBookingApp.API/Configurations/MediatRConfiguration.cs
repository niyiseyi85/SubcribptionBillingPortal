using FluentValidation;
using MediatR;
using VeltrixBookingApp.Application.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace VeltrixBookingApp.API.Configurations
{
    internal interface IApplicationAssemblyMarker { }

    internal static class MediatRConfiguration
    {
        internal static IServiceCollection AddMediatRConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(IApplicationAssemblyMarker).Assembly);
            });

            services.AddValidatorsFromAssembly(typeof(IApplicationAssemblyMarker).Assembly);

            // Register pipeline behaviors (logging, validation)
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));


            return services;
        }
    }
}
