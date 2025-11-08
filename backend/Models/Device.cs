using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HackathonBackend.Models
{
    // Таблица устройств
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

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Key]
        [Column("device_code")]
        [MaxLength(50)]
        public string DeviceCode { get; set; } = string.Empty;
    }
}
