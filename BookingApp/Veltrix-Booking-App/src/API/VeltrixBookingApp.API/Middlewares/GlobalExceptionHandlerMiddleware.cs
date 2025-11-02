using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using VeltrixBookingApp.Application.Common.BasicResult;

namespace VeltrixBookingApp.API.Middlewares
{
    public static class ExceptionHandlerExtension
    {
        public static void ConfigureExceptionHandler(this IApplicationBuilder app, ILogger logger)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "application/json";
                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature != null)
                    {
                        var errorDetails = new BasicActionResult<string>("An unexpected error occurred. Please try again later", HttpStatusCode.ServiceUnavailable);

                        // Check if the environment is Development
                        var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
                        if (env.IsDevelopment())
                        {
                            errorDetails.Message = contextFeature.Error.Message;
                        }

                        // Log the exception
                        logger.LogError($"Exception was thrown and handled: {contextFeature.Error}");

                        await context.Response.WriteAsJsonAsync(errorDetails);
                    }
                });
            });
        }
    }
}
