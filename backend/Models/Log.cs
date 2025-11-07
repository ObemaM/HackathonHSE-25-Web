using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HackathonBackend.Models
{
    /// <summary>
    /// Log представляет таблицу logs
    /// </summary>
    [Table("logs")]
    public class Log
    {
        [Key]
        [Column("action_code")]
        [MaxLength(20)]
        public string ActionCode { get; set; } = string.Empty;

        [Key]
        [Column("app_version")]
        [MaxLength(20)]
        public string AppVersion { get; set; } = string.Empty;

        [Key]
        [Column("device_code")]
        [MaxLength(50)]
        public string DeviceCode { get; set; } = string.Empty;

        [Column("team_number")]
        [Required]
        [MaxLength(50)]
        public string TeamNumber { get; set; } = string.Empty;

        [Key]
        [Column("datetime")]
        public DateTime Datetime { get; set; }
    }
}
