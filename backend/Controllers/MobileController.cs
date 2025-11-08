using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HackathonBackend.Data;
using HackathonBackend.Models;
using HackathonBackend.Models.DTOs;
using System.Text.Json;
using System.Globalization;

namespace HackathonBackend.Controllers
{
    /// <summary>
    /// MobileController обрабатывает запросы с мобильных устройств
    /// </summary>
    [ApiController]
    [Route("api/mobile")]
    public class MobileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MobileController> _logger;

        public MobileController(ApplicationDbContext context, ILogger<MobileController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Добавляет лог с мобильного устройства
        /// </summary>
        [HttpPost("log")]
        public async Task<IActionResult> AddLog([FromBody] MobileLogRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // 1. Проверяем/создаем СМП
                var smp = await _context.SMPs
                    .FirstOrDefaultAsync(s => s.RegionCode == request.RegionCode && s.SmpCode == request.SmpCode);
                
                if (smp == null)
                {
                    smp = new SMP
                    {
                        RegionCode = request.RegionCode,
                        SmpCode = request.SmpCode
                    };
                    _context.SMPs.Add(smp);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Создан новый СМП: {request.RegionCode}/{request.SmpCode}");
                }

                // 2. Проверяем/создаем устройство
                var device = await _context.Devices
                    .FirstOrDefaultAsync(d => d.DeviceCode == request.DeviceCode);
                
                if (device == null)
                {
                    device = new Device
                    {
                        DeviceCode = request.DeviceCode,
                        RegionCode = request.RegionCode,
                        SmpCode = request.SmpCode,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Devices.Add(device);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Создано новое устройство: {request.DeviceCode}");
                }
                else if (device.RegionCode != request.RegionCode || device.SmpCode != request.SmpCode)
                {
                    // Обновляем регион и СМП устройства если они изменились
                    device.RegionCode = request.RegionCode;
                    device.SmpCode = request.SmpCode;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Обновлены данные устройства: {request.DeviceCode}");
                }

                // 3. Проверяем/создаем действие
                var action = await _context.Actions
                    .FirstOrDefaultAsync(a => a.ActionCode == request.ActionCode && a.AppVersion == request.AppVersion);
                
                if (action == null)
                {
                    action = new Models.Action
                    {
                        ActionCode = request.ActionCode,
                        AppVersion = request.AppVersion,
                        ActionText = request.ActionText ?? $"Действие {request.ActionCode}"
                    };
                    _context.Actions.Add(action);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Создано новое действие: {request.ActionCode} v{request.AppVersion}");
                }

                // 4. Создаем лог
                var datetime = request.Datetime ?? DateTime.UtcNow;
                var log = new Log
                {
                    ActionCode = request.ActionCode,
                    AppVersion = request.AppVersion,
                    DeviceCode = request.DeviceCode,
                    TeamNumber = request.TeamNumber,
                    Datetime = datetime
                };

                _context.Logs.Add(log);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Добавлен лог: {request.DeviceCode} - {request.ActionCode} в {datetime}");

                return Ok(new
                {
                    success = true,
                    message = "Лог успешно добавлен",
                    data = new
                    {
                        device_code = request.DeviceCode,
                        action_code = request.ActionCode,
                        datetime = datetime
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при добавлении лога: {ex.Message}");
                return StatusCode(500, new { error = $"Ошибка при добавлении лога: {ex.Message}" });
            }
        }

        /// <summary>
        /// Добавляет пакет логов с мобильного устройства. Поддерживает два формата тела запроса:
        /// 1) Объект: { "logs": [ {..}, {..} ] }
        /// 2) Массив: [ {..}, {..} ]
        /// </summary>
        [HttpPost("logs/batch")]
        public async Task<IActionResult> AddLogsBatch([FromBody] JsonElement body)
        {
            List<JsonElement> elements = new List<JsonElement>();

            try
            {
                if (body.ValueKind == JsonValueKind.Object)
                {
                    if (body.TryGetProperty("logs", out var logsProp) && logsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var el in logsProp.EnumerateArray()) elements.Add(el);
                    }
                    else
                    {
                        return BadRequest(new { error = "Ожидалось поле 'logs' с массивом логов" });
                    }
                }
                else if (body.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in body.EnumerateArray()) elements.Add(el);
                }
                else
                {
                    return BadRequest(new { error = "Ожидался объект с 'logs' или массив логов" });
                }

                if (elements.Count == 0)
                {
                    return BadRequest(new { error = "Нет логов для добавления" });
                }

                var results = new List<object>();
                var successCount = 0;
                var failCount = 0;

                foreach (var el in elements)
                {
                    try
                    {
                        // Извлекаем поля вручную, чтобы лояльно парсить дату
                        string regionCode = el.TryGetProperty("region_code", out var rc) ? rc.GetString() ?? string.Empty : string.Empty;
                        string smpCode = el.TryGetProperty("smp_code", out var sc) ? sc.GetString() ?? string.Empty : string.Empty;
                        string deviceCode = el.TryGetProperty("device_code", out var dc) ? dc.GetString() ?? string.Empty : string.Empty;
                        string teamNumber = el.TryGetProperty("team_number", out var tn) ? tn.GetString() ?? string.Empty : string.Empty;
                        string actionCode = el.TryGetProperty("action_code", out var ac) ? ac.GetString() ?? string.Empty : string.Empty;
                        string appVersion = el.TryGetProperty("app_version", out var av) ? av.GetString() ?? string.Empty : string.Empty;
                        string? actionText = el.TryGetProperty("action_text", out var at) ? at.GetString() : null;

                        DateTime datetime = DateTime.UtcNow;
                        if (el.TryGetProperty("datetime", out var dtProp))
                        {
                            if (dtProp.ValueKind == JsonValueKind.String)
                            {
                                var dtStr = dtProp.GetString();
                                // Пытаемся распарсить ISO или формат "yyyy-MM-dd HH:mm:ss.ffffff"
                                if (!string.IsNullOrWhiteSpace(dtStr))
                                {
                                    if (!DateTime.TryParse(dtStr, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out datetime))
                                    {
                                        DateTime.TryParseExact(dtStr,
                                            new[] { "yyyy-MM-dd HH:mm:ss.ffffff", "yyyy-MM-dd HH:mm:ss" },
                                            CultureInfo.InvariantCulture,
                                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                                            out datetime);
                                    }
                                }
                            }
                            else if (dtProp.ValueKind == JsonValueKind.Number && dtProp.TryGetInt64(out var unixMs))
                            {
                                datetime = DateTimeOffset.FromUnixTimeMilliseconds(unixMs).UtcDateTime;
                            }
                        }

                        // Проверяем/создаем СМП
                        var smp = await _context.SMPs
                            .FirstOrDefaultAsync(s => s.RegionCode == regionCode && s.SmpCode == smpCode);
                        if (smp == null)
                        {
                            smp = new SMP { RegionCode = regionCode, SmpCode = smpCode };
                            _context.SMPs.Add(smp);
                        }

                        // Проверяем/создаем устройство
                        var device = await _context.Devices.FirstOrDefaultAsync(d => d.DeviceCode == deviceCode);
                        if (device == null)
                        {
                            device = new Device
                            {
                                DeviceCode = deviceCode,
                                RegionCode = regionCode,
                                SmpCode = smpCode,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.Devices.Add(device);
                        }

                        // Проверяем/создаем действие
                        var action = await _context.Actions.FirstOrDefaultAsync(a => a.ActionCode == actionCode && a.AppVersion == appVersion);
                        if (action == null)
                        {
                            action = new Models.Action
                            {
                                ActionCode = actionCode,
                                AppVersion = appVersion,
                                ActionText = actionText ?? $"Действие {actionCode}"
                            };
                            _context.Actions.Add(action);
                        }

                        // Создаем лог
                        var log = new Log
                        {
                            ActionCode = actionCode,
                            AppVersion = appVersion,
                            DeviceCode = deviceCode,
                            TeamNumber = teamNumber,
                            Datetime = datetime
                        };

                        _context.Logs.Add(log);
                        await _context.SaveChangesAsync();

                        successCount++;
                        results.Add(new { success = true, device_code = deviceCode, action_code = actionCode });
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        _logger.LogError($"Ошибка при добавлении лога: {ex.Message}");
                        results.Add(new { success = false, error = ex.Message });
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = $"Обработано {elements.Count} логов. Успешно: {successCount}, Ошибок: {failCount}",
                    results = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка обработки тела запроса: {ex.Message}");
                return BadRequest(new { error = $"Неверный формат запроса: {ex.Message}" });
            }
        }

        /// <summary>
        /// Тестовый endpoint для проверки связи
        /// </summary>
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new
            {
                message = "Мобильный API работает",
                status = "success",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
