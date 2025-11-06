using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HackathonBackend.Models
{
    /// <summary>
    /// Action представляет таблицу actions
    /// </summary>
    [Table("actions")]
    public class Action
    {
        [Column("action_text")]
        [Required]
        [MaxLength(500)]
        public string ActionText { get; set; } = string.Empty;

        [Key]
        [Column("action_code")]
        [MaxLength(20)]
        public string ActionCode { get; set; } = string.Empty;

        [Key]
        [Column("app_version")]
        [MaxLength(20)]
        public string AppVersion { get; set; } = string.Empty;
    }
}
