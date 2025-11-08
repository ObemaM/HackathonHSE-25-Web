using System.ComponentModel.DataAnnotations;

namespace HackathonBackend.Models.DTOs
{
    public class LoginRequest // Запрос на вход
    {
        [Required]
        public string Login { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
