using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SubscriptionBillingPortal.Domain.Exceptions;
using System.Net;

namespace SubscriptionBillingPortal.API.Middleware;

/// <summary>
/// Global exception handler using the .NET 8+ IExceptionHandler pattern.
/// Integrates with AddProblemDetails() and UseExceptionHandler() for fully
/// framework-native RFC 7807 ProblemDetails error responses.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail, extensions) = MapException(exception, httpContext);

        LogException(exception, httpContext, statusCode);

        httpContext.Response.StatusCode = (int)statusCode;

        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{(int)statusCode}",
            Title = title,
            Status = (int)statusCode,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (extensions is not null)
        {
            foreach (var (key, value) in extensions)
            {
                problemDetails.Extensions[key] = value;
            }
        }

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }

    private static (HttpStatusCode StatusCode, string Title, string Detail, Dictionary<string, object?>? Extensions)
        MapException(Exception exception, HttpContext httpContext) => exception switch
    {
        ValidationException ex => (
            HttpStatusCode.BadRequest,
            "Validation Error",
            "One or more validation errors occurred.",
            new Dictionary<string, object?>
            {
                ["errors"] = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            }),

        DomainException ex => (
            HttpStatusCode.UnprocessableEntity,
            "Domain Rule Violation",
            ex.Message,
            null),

        KeyNotFoundException ex => (
            HttpStatusCode.NotFound,
            "Resource Not Found",
            ex.Message,
            null),

        InvalidOperationException ex => (
            HttpStatusCode.Conflict,
            "Conflict",
            ex.Message,
            null),

        _ => (
            HttpStatusCode.InternalServerError,
            "Internal Server Error",
            "An unexpected error occurred. Please try again later.",
            null)
    };

    private void LogException(Exception exception, HttpContext httpContext, HttpStatusCode statusCode)
    {
        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled exception on request {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "{ExceptionType} on request {Method} {Path}",
                exception.GetType().Name,
                httpContext.Request.Method,
                httpContext.Request.Path);
        }
    }
}
