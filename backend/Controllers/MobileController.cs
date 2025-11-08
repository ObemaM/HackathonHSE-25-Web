using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HackathonBackend.Data;
using HackathonBackend.Models;
using HackathonBackend.Models.DTOs;
using System.Text.Json;
using System.Globalization;

namespace HackathonBackend.Controllers
{
    [ApiController]
    [Route("api/mobile")] // Все запросы с мобильных устройств
    public class MobileController : ControllerBase
    {
        private readonly ApplicationDbContext tocontext;
        private readonly ILogger<MobileController> tologger;

        public MobileController(ApplicationDbContext context, ILogger<MobileController> logger)
        {
            tocontext = context;
            tologger = logger;
        }

        [HttpPost("log")] // Добавляем лог с мобильного устройства
        public async Task<IActionResult> AddLog([FromBody] MobileLogRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var smp = await tocontext.SMPs // Проверяем и создаем СМП
                    .FirstOrDefaultAsync(s => s.RegionCode == request.RegionCode && s.SmpCode == request.SmpCode);
                
                if (smp == null) // Если нет - создаем (и сохраняем в БД)
                {
                    smp = new SMP
                    {
                        RegionCode = request.RegionCode,
                        SmpCode = request.SmpCode
                    };
                    tocontext.SMPs.Add(smp);
                    await tocontext.SaveChangesAsync();
                }

                var device = await tocontext.Devices // Проверяем и создаем новое устройство
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
                    tocontext.Devices.Add(device);
                    await tocontext.SaveChangesAsync();
                }

                else if (device.RegionCode != request.RegionCode || device.SmpCode != request.SmpCode) // Обновляем регион и СМП устройства если они изменились
                {
                    device.RegionCode = request.RegionCode;
                    device.SmpCode = request.SmpCode;

                    var logsToUpdate = await tocontext.Logs // Обновляем логи устройства, если почистили кэш на устройстве
                        .Where(l => l.DeviceCode == device.DeviceCode)
                        .ToListAsync();

                    await tocontext.SaveChangesAsync();
                }

                var action = await tocontext.Actions // Проверяем и создаем действие
                    .FirstOrDefaultAsync(a => a.ActionCode == request.ActionCode && a.AppVersion == request.AppVersion);
                
                if (action == null)
                {
                    action = new Models.Action
                    {
                        ActionCode = request.ActionCode,
                        AppVersion = request.AppVersion,
                        ActionText = request.ActionText ?? $"Действие {request.ActionCode}"
                    };
                    tocontext.Actions.Add(action);
                    await tocontext.SaveChangesAsync();
                }

                var datetime = request.Datetime ?? DateTime.UtcNow; // Создаем лог, если его нет
                var log = new Log
                {
                    ActionCode = request.ActionCode,
                    AppVersion = request.AppVersion,
                    DeviceCode = request.DeviceCode,
                    TeamNumber = request.TeamNumber,
                    Datetime = datetime
                };

                tocontext.Logs.Add(log);
                await tocontext.SaveChangesAsync();

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
                tologger.LogError($"Ошибка при добавлении лога: {ex.Message}");
                return StatusCode(500, new { error = $"Ошибка при добавлении лога: {ex.Message}" });
            }
        }

        // Добавляет пакет логов с телефона и тут идеи обработка
        [HttpPost("logs/batch")]
        public async Task<IActionResult> AddLogsBatch([FromBody] JsonElement body)
        {
            List<JsonElement> elements = new List<JsonElement>();

            try
            {
                if (body.ValueKind != JsonValueKind.Array)
                {
                    return BadRequest(new { error = "Ожидался массив логов [ {..}, {..} ]" });
                }

                foreach (var el in body.EnumerateArray()) elements.Add(el);

                var results = new List<object>();
                var successCount = 0;
                var failCount = 0;

                foreach (var el in elements)
                {
                    try
                    {
                        // Извлекаем поля вручную
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
                                var dtStr = dtProp.GetString(); // Пытаемся распарсить дату
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

                        var smp = await tocontext.SMPs // Проверяем СМП
                            .FirstOrDefaultAsync(s => s.RegionCode == regionCode && s.SmpCode == smpCode);
                        if (smp == null)
                        {
                            smp = new SMP { RegionCode = regionCode, SmpCode = smpCode };
                            tocontext.SMPs.Add(smp);
                        }

                        // Проверяем устройство
                        var device = await tocontext.Devices.FirstOrDefaultAsync(d => d.DeviceCode == deviceCode);
                        {
                            device = new Device
                            {
                                DeviceCode = deviceCode,
                                RegionCode = regionCode,
                                SmpCode = smpCode,
                                CreatedAt = DateTime.UtcNow
                            };
                            tocontext.Devices.Add(device);
                        }

                        // Проверяем действие
                        var action = await tocontext.Actions.FirstOrDefaultAsync(a => a.ActionCode == actionCode && a.AppVersion == appVersion);

                        var log = new Log // Создаем лог
                        {
                            ActionCode = actionCode,
                            AppVersion = appVersion,
                            DeviceCode = deviceCode,
                            TeamNumber = teamNumber,
                            Datetime = datetime
                        };

                        tocontext.Logs.Add(log);
                        await tocontext.SaveChangesAsync();

                        successCount++;
                        results.Add(new { success = true, device_code = deviceCode, action_code = actionCode });
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        tologger.LogError($"Ошибка при добавлении лога: {ex.Message}");
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
                tologger.LogError($"Ошибка обработки тела запроса: {ex.Message}");
                return BadRequest(new { error = $"Неверный формат запроса: {ex.Message}" });
            }
        }

        [HttpGet("test")] // Тестовый, для Дениса
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
