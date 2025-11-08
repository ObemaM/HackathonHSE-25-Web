using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HackathonBackend.Data;
using HackathonBackend.Models;
using HackathonBackend.Models.DTOs;

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
        /// Добавляет пакет логов с мобильного устройства
        /// </summary>
        [HttpPost("logs/batch")]
        public async Task<IActionResult> AddLogsBatch([FromBody] MobileLogBatchRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (request.Logs == null || request.Logs.Count == 0)
            {
                return BadRequest(new { error = "Нет логов для добавления" });
            }

            var results = new List<object>();
            var successCount = 0;
            var failCount = 0;

            foreach (var logRequest in request.Logs)
            {
                try
                {
                    // Проверяем/создаем СМП
                    var smp = await _context.SMPs
                        .FirstOrDefaultAsync(s => s.RegionCode == logRequest.RegionCode && s.SmpCode == logRequest.SmpCode);
                    
                    if (smp == null)
                    {
                        smp = new SMP
                        {
                            RegionCode = logRequest.RegionCode,
                            SmpCode = logRequest.SmpCode
                        };
                        _context.SMPs.Add(smp);
                    }

                    // Проверяем/создаем устройство
                    var device = await _context.Devices
                        .FirstOrDefaultAsync(d => d.DeviceCode == logRequest.DeviceCode);
                    
                    if (device == null)
                    {
                        device = new Device
                        {
                            DeviceCode = logRequest.DeviceCode,
                            RegionCode = logRequest.RegionCode,
                            SmpCode = logRequest.SmpCode,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Devices.Add(device);
                    }

                    // Проверяем/создаем действие
                    var action = await _context.Actions
                        .FirstOrDefaultAsync(a => a.ActionCode == logRequest.ActionCode && a.AppVersion == logRequest.AppVersion);
                    
                    if (action == null)
                    {
                        action = new Models.Action
                        {
                            ActionCode = logRequest.ActionCode,
                            AppVersion = logRequest.AppVersion,
                            ActionText = logRequest.ActionText ?? $"Действие {logRequest.ActionCode}"
                        };
                        _context.Actions.Add(action);
                    }

                    // Создаем лог
                    var datetime = logRequest.Datetime ?? DateTime.UtcNow;
                    var log = new Log
                    {
                        ActionCode = logRequest.ActionCode,
                        AppVersion = logRequest.AppVersion,
                        DeviceCode = logRequest.DeviceCode,
                        TeamNumber = logRequest.TeamNumber,
                        Datetime = datetime
                    };

                    _context.Logs.Add(log);
                    await _context.SaveChangesAsync();

                    successCount++;
                    results.Add(new
                    {
                        success = true,
                        device_code = logRequest.DeviceCode,
                        action_code = logRequest.ActionCode
                    });
                }
                catch (Exception ex)
                {
                    failCount++;
                    _logger.LogError($"Ошибка при добавлении лога: {ex.Message}");
                    results.Add(new
                    {
                        success = false,
                        device_code = logRequest.DeviceCode,
                        error = ex.Message
                    });
                }
            }

            return Ok(new
            {
                success = true,
                message = $"Обработано {request.Logs.Count} логов. Успешно: {successCount}, Ошибок: {failCount}",
                results = results
            });
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
