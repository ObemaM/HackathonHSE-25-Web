using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HackathonBackend.Models
{
    /// <summary>
    /// Device представляет таблицу devices
    /// </summary>
    [Table("devices")]
    public class Device
    {
        [Column("region_code")]
        [Required]
        [MaxLength(50)]
        public string RegionCode { get; set; } = string.Empty;

        [Column("smp_code")]
        [Required]
        [MaxLength(50)]
        public string SmpCode { get; set; } = string.Empty;

        [Column("team_number")]
        [Required]
        [MaxLength(50)]
        public string TeamNumber { get; set; } = string.Empty;

        [Column("created_at")]
        [Required]
        public DateTime CreatedAt { get; set; }

        [Key]
        [Column("device_code")]
        [MaxLength(50)]
        public string DeviceCode { get; set; } = string.Empty;
    }
}
