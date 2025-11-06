using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace HackathonBackend.Models
{
    /// <summary>
    /// SMP представляет таблицу smp
    /// </summary>
    [Table("smp")]
    public class SMP
    {
        [Key]
        [Column("region_code")]
        [MaxLength(50)]
        [JsonPropertyName("region_code")]
        public string RegionCode { get; set; } = string.Empty;

        [Key]
        [Column("smp_code")]
        [MaxLength(50)]
        [JsonPropertyName("smp_code")]
        public string SmpCode { get; set; } = string.Empty;
    }
}
