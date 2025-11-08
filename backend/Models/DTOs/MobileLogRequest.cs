using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HackathonBackend.Models.DTOs
{
    public class MobileLogRequest // Запрос на добавление лога с мобильного устройства
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
}
