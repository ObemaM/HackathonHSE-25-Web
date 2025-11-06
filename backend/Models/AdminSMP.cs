using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HackathonBackend.Models
{
    /// <summary>
    /// AdminSMP представляет связь между администратором и СМП
    /// </summary>
    [Table("admins_smp")]
    public class AdminSMP
    {
        [Key]
        [Column("login")]
        [MaxLength(50)]
        public string Login { get; set; } = string.Empty;

        [Key]
        [Column("region_code")]
        [MaxLength(50)]
        public string RegionCode { get; set; } = string.Empty;

        [Key]
        [Column("smp_code")]
        [MaxLength(50)]
        public string SmpCode { get; set; } = string.Empty;
    }
}
