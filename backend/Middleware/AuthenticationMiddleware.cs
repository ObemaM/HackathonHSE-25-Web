using HackathonBackend.Utils;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace HackathonBackend.Middleware
{
    /// <summary>
    /// AuthenticationMiddleware проверяет аутентификацию пользователя
    /// </summary>
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var user = context.Session.GetCurrentUser();
            
            if (string.IsNullOrEmpty(user))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                
                var response = new { error = "Требуется аутентификация" };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                return;
            }

            await _next(context);
        }
    }

    /// <summary>
    /// Расширение для удобного добавления middleware
    /// </summary>
    public static class AuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthenticationMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthenticationMiddleware>();
        }
    }
}
