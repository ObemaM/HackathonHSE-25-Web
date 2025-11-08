using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HackathonBackend.Models.DTOs
{
    /// <summary>
    /// MobileLogRequest представляет запрос на добавление лога с мобильного устройства
    /// </summary>
    public class MobileLogRequest
    {
        [JsonPropertyName("region_code")]
        [Required(ErrorMessage = "Код региона обязателен")]
        [MaxLength(50)]
        public string RegionCode { get; set; } = string.Empty;

        [JsonPropertyName("smp_code")]
        [Required(ErrorMessage = "Код СМП обязателен")]
        [MaxLength(50)]
        public string SmpCode { get; set; } = string.Empty;

        [JsonPropertyName("team_number")]
        [Required(ErrorMessage = "Номер бригады обязателен")]
        [MaxLength(50)]
        public string TeamNumber { get; set; } = string.Empty;

        [JsonPropertyName("device_code")]
        [Required(ErrorMessage = "Идентификатор устройства обязателен")]
        [MaxLength(50)]
        public string DeviceCode { get; set; } = string.Empty;

        [JsonPropertyName("app_version")]
        [Required(ErrorMessage = "Версия приложения обязательна")]
        [MaxLength(20)]
        public string AppVersion { get; set; } = string.Empty;

        [JsonPropertyName("action_code")]
        [Required(ErrorMessage = "Код действия обязателен")]
        [MaxLength(20)]
        public string ActionCode { get; set; } = string.Empty;

        [JsonPropertyName("action_text")]
        public string? ActionText { get; set; }

        [JsonPropertyName("datetime")]
        public DateTime? Datetime { get; set; }
    }

    /// <summary>
    /// MobileLogBatchRequest представляет запрос на добавление пакета логов с мобильного устройства
    /// </summary>
    public class MobileLogBatchRequest
    {
        [JsonPropertyName("logs")]
        [Required]
        public List<MobileLogRequest> Logs { get; set; } = new List<MobileLogRequest>();
    }
}
