using System.ComponentModel.DataAnnotations;

namespace HackathonBackend.Models.DTOs
{
    /// <summary>
    /// MobileLogRequest представляет запрос на добавление лога с мобильного устройства
    /// </summary>
    public class MobileLogRequest
    {
        [Required(ErrorMessage = "Код региона обязателен")]
        [MaxLength(50)]
        public string RegionCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Код СМП обязателен")]
        [MaxLength(50)]
        public string SmpCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Номер бригады обязателен")]
        [MaxLength(50)]
        public string TeamNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Идентификатор устройства обязателен")]
        [MaxLength(50)]
        public string DeviceCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Версия приложения обязательна")]
        [MaxLength(20)]
        public string AppVersion { get; set; } = string.Empty;

        [Required(ErrorMessage = "Код действия обязателен")]
        [MaxLength(20)]
        public string ActionCode { get; set; } = string.Empty;

        public string? ActionText { get; set; }

        public DateTime? Datetime { get; set; }
    }

    /// <summary>
    /// MobileLogBatchRequest представляет запрос на добавление пакета логов с мобильного устройства
    /// </summary>
    public class MobileLogBatchRequest
    {
        [Required]
        public List<MobileLogRequest> Logs { get; set; } = new List<MobileLogRequest>();
    }
}
