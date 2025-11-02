using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace VeltrixBookingApp.Application.Commands
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
        where TResponse : notnull
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));

            var requestName = typeof(TRequest).Name;
            _logger.LogInformation("Handling {RequestName}: {@Request}", requestName, request);

            var response = await next().ConfigureAwait(false);

            _logger.LogInformation("Handled {RequestName} -> {Response}", requestName, response);
            
            return response;
        }
    }
}
