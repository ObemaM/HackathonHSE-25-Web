using Microsoft.AspNetCore.Http;

namespace HackathonBackend.Utils
{
    /// <summary>
    /// SessionExtensions предоставляет вспомогательные методы для работы с сессией
    /// </summary>
    public static class SessionExtensions
    {
        private const string UserSessionKey = "user";

        /// <summary>
        /// Устанавливает сессию пользователя
        /// </summary>
        public static void SetUserSession(this ISession session, string login)
        {
            session.SetString(UserSessionKey, login);
        }

        /// <summary>
        /// Получает логин текущего пользователя из сессии
        /// </summary>
        public static string? GetCurrentUser(this ISession session)
        {
            return session.GetString(UserSessionKey);
        }

        /// <summary>
        /// Удаляет сессию пользователя
        /// </summary>
        public static void ClearUserSession(this ISession session)
        {
            session.Remove(UserSessionKey);
        }
    }
}
