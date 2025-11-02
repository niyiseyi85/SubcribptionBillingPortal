using System.Net;
using System.Text.Json;

namespace VeltrixBookingApp.API.Middlewares
{
    internal class UnauthorizedRequestMiddleware
    {
        private readonly RequestDelegate _next;

        public UnauthorizedRequestMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            await _next(context);

            if (context.Response.StatusCode == (int)HttpStatusCode.Unauthorized)
            {
                context.Response.ContentType = "application/json";
                var payload = JsonSerializer.Serialize(new { Message = "Unauthorized" });
                await context.Response.WriteAsync(payload);
            }
        }
    }
}
