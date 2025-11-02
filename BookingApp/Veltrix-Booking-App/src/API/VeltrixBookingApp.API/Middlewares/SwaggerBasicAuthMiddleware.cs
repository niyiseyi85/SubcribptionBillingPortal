using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using VeltrixBookingApp.API.Configurations;

namespace VeltrixBookingApp.API.Middlewares
{
    internal sealed class SwaggerBasicAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly SwaggerAuthSettings _settings;

        public SwaggerBasicAuthMiddleware(RequestDelegate next, IOptions<SwaggerAuthSettings> settings)
        {
            _next = next;
            _settings = settings.Value;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase))
            {
                var authHeader = context.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Basic ", StringComparison.Ordinal))
                {
                    var encodedUsernamePassword = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1)?.Trim();
                    if (string.IsNullOrEmpty(encodedUsernamePassword))
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        return;
                    }

                    var decodedUsernamePassword = Encoding.UTF8.GetString(Convert.FromBase64String(encodedUsernamePassword));

                    var username = decodedUsernamePassword.Split(':', 2)[0];
                    var password = decodedUsernamePassword.Split(':', 2)[1];

                    if (IsAuthorized(username, password))
                    {
                        await _next.Invoke(context);
                        return;
                    }
                }

                context.Response.Headers["WWW-Authenticate"] = "Basic";

                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            }
            else
            {
                await _next.Invoke(context);
            }
        }

        public bool IsAuthorized(string username, string password)
        {
            var _userName = _settings.SwaggerAuthUsername;
            var _password = _settings.SwaggerAuthPassword;

            return username.Equals(_userName, StringComparison.OrdinalIgnoreCase) && password.Equals(_password, StringComparison.Ordinal);
        }
    }
}
