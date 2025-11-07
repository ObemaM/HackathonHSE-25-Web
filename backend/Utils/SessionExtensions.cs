using Microsoft.AspNetCore.Http;

namespace HackathonBackend.Utils
{
    // Сессия пользователя
    public static class SessionExtensions
    {
        private const string UserSessionKey = "user";

        // Устанавливает логин текущего пользователя в сессию
        public static void SetUserSession(this ISession session, string login)
        {
            session.SetString(UserSessionKey, login);
        }

        // Получает логин текущего пользователя из сессии
        public static string? GetCurrentUser(this ISession session)
        {
            return session.GetString(UserSessionKey);
        }

        // Удаляет сессию пользователя
        public static void ClearUserSession(this ISession session)
        {
            session.Remove(UserSessionKey);
        }
    }
}
