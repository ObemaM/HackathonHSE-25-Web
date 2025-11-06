using System.ComponentModel.DataAnnotations;

namespace HackathonBackend.Models.DTOs
{
    /// <summary>
    /// LoginRequest представляет структуру запроса на вход
    /// </summary>
    public class LoginRequest
    {
        [Required]
        public string Login { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
