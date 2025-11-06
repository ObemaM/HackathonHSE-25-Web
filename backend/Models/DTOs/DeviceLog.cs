using System.Text.Json.Serialization;

namespace HackathonBackend.Models.DTOs
{
    public class DeviceLog
    {
        [JsonPropertyName("action_code")]
        public string ActionCode { get; set; } = string.Empty;

        [JsonPropertyName("app_version")]
        public string AppVersion { get; set; } = string.Empty;

        [JsonPropertyName("device_code")]
        public string DeviceCode { get; set; } = string.Empty;

        [JsonPropertyName("datetime")]
        public DateTime Datetime { get; set; }

        [JsonPropertyName("region_code")]
        public string RegionCode { get; set; } = string.Empty;

        [JsonPropertyName("smp_code")]
        public string SMPCode { get; set; } = string.Empty;

        [JsonPropertyName("team_number")]
        public string TeamNumber { get; set; } = string.Empty;

        [JsonPropertyName("action_text")]
        public string ActionText { get; set; } = string.Empty;
    }
}
