namespace HackathonBackend.Utils
{
    /// <summary>
    /// PasswordHelper обрабатывает хеширование и проверку паролей с использованием BCrypt
    /// </summary>
    public static class PasswordHelper
    {
        /// <summary>
        /// HashPassword хеширует пароль с использованием bcrypt
        /// </summary>
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, 12);
        }

        /// <summary>
        /// CheckPasswordHash сравнивает пароль с хешем
        /// </summary>
        public static bool CheckPasswordHash(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }
    }
}
