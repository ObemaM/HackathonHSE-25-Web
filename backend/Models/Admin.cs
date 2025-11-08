using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HackathonBackend.Models
{
    // Таблица админов
    [Table("admins")]
    public class Admin
    {
        [Key]
        [Column("login")]
        [MaxLength(50)]
        public string Login { get; set; } = string.Empty;

        [Column("password_hash")]
        [MaxLength(70)]
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("created_at")]
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
