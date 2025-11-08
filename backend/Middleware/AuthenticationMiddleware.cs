using HackathonBackend.Utils;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace HackathonBackend.Middleware
{
    // AuthenticationMiddleware проверяет аутентификацию пользователя
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate tonext;

        public AuthenticationMiddleware(RequestDelegate next)
        {
            tonext = next;
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

            await tonext(context);
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
