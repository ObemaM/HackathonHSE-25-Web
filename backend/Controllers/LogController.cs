using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HackathonBackend.Data;
using HackathonBackend.Models;
using HackathonBackend.Models.DTOs;
using HackathonBackend.Utils;
using static HackathonBackend.Utils.LINQBuilder;

namespace HackathonBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Обработка всех контроллеров связанных с логами
    public class LogsController : ControllerBase // Для обработки логов
    {
        private readonly ApplicationDbContext tocontext;

        public LogsController(ApplicationDbContext context)
        {
            tocontext = context;
        }

        private List<string> GetAccessibleDeviceCodes(string login) // Список кодов устройств, доступных админу
        {
            var permissions = tocontext.AdminSMPs // Получаем права доступа админа (по региону и смп)
                .Where(a => a.Login == login)
                .Select(p => new { p.RegionCode, p.SmpCode })
                .ToList();

            if (permissions.Count == 0) // Если нет прав доступа
            {
                return new List<string>(); // Возвращаем пустой список (возвращаемый метод)
            }

            var predicate = False<Device>(); // Создаем OR условия для каждой пары регион-СМП
            foreach (var perm in permissions)
            {
                var regionCode = perm.RegionCode;
                var smpCode = perm.SmpCode;
                predicate = predicate.Or(d => d.RegionCode == regionCode && d.SmpCode == smpCode);
            }

            var deviceCodes = tocontext.Devices
                .Where(predicate)
                .Select(d => d.DeviceCode)
                .ToList();

            return deviceCodes;
        }

        private IQueryable<Log> ApplyAdminFilters(IQueryable<Log> query) // Применяет фильтры к запросу логов
        {
            var login = HttpContext.Session.GetCurrentUser();
            if (string.IsNullOrEmpty(login))
            {
                return query.Where(l => false);
            }

            var accessibleDeviceCodes = GetAccessibleDeviceCodes(login);
            if (accessibleDeviceCodes.Count == 0)
            {
                return query.Where(l => false);
            }

            return query.Where(l => accessibleDeviceCodes.Contains(l.DeviceCode));
        }

        [HttpGet("unique-values")] // Возвращает уникальные значения для фильтров
        public async Task<IActionResult> GetUniqueValues()
        {
            var login = HttpContext.Session.GetCurrentUser();
            if (string.IsNullOrEmpty(login))
            {
                return Unauthorized(new { error = "Требуется авторизация" });
            }

            try
            {
                var accessibleDeviceCodes = GetAccessibleDeviceCodes(login); // Получаем доступные устройства
                if (accessibleDeviceCodes.Count == 0)
                {
                    return Ok(new { }); // Если нет доступных устройств, возвращаем пустой объект
                }

                var regions = await tocontext.Devices // Уникальные коды регионов
                    .Where(d => accessibleDeviceCodes.Contains(d.DeviceCode))
                    .Where(d => !string.IsNullOrEmpty(d.RegionCode))
                    .Select(d => d.RegionCode)
                    .Distinct()
                    .ToListAsync();

                var smpCodes = await tocontext.Devices // Уникальные коды СМП
                    .Where(d => accessibleDeviceCodes.Contains(d.DeviceCode))
                    .Where(d => !string.IsNullOrEmpty(d.SmpCode))
                    .Select(d => d.SmpCode)
                    .Distinct()
                    .ToListAsync();

                var teamNumbers = await tocontext.Logs // Уникальные номера команд
                    .Where(d => accessibleDeviceCodes.Contains(d.DeviceCode))
                    .Where(d => !string.IsNullOrEmpty(d.TeamNumber))
                    .Select(d => d.TeamNumber)
                    .Distinct()
                    .ToListAsync();

                var appVersions = await tocontext.Logs // Уникальные версии приложения
                    .Where(l => accessibleDeviceCodes.Contains(l.DeviceCode))
                    .Where(l => !string.IsNullOrEmpty(l.AppVersion))
                    .Select(l => l.AppVersion)
                    .Distinct()
                    .ToListAsync();

                var actionTexts = await tocontext.Actions // Уникальные тексты действий
                    .Where(a => !string.IsNullOrEmpty(a.ActionText))
                    .Select(a => a.ActionText)
                    .Distinct()
                    .ToListAsync();

                return Ok(new // Возвращаем объект с уникальными значениями
                {
                    region_code = regions,
                    smp_code = smpCodes,
                    team_number = teamNumbers,
                    app_version = appVersions,
                    action_text = actionTexts
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при получении уникальных значений: {ex.Message}" });
            }
        }

        [HttpGet] // Логи, которые доступны текущему админу
        public async Task<IActionResult> GetAllLogs()
        {
            try
            {
                var query = tocontext.Logs.OrderByDescending(l => l.Datetime); // Сортируем логи по дате (по убыванию)
                var filteredQuery = ApplyAdminFilters(query); // Применяем фильтры
                var logs = await filteredQuery.ToListAsync(); // Получаем логи
                return Ok(logs); // Возвращаем логи
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при получении логов: {ex.Message}" });
            }
        }

        [HttpGet("latest")] // Последние логи для каждого устройства, доступного текущему админу
        public async Task<IActionResult> GetLatestDeviceLogs()
        {
            var login = HttpContext.Session.GetCurrentUser();
            if (string.IsNullOrEmpty(login))
            {
                return Unauthorized(new { error = "Требуется аутентификация" });
            }

            try
            {
                var accessibleDeviceCodes = GetAccessibleDeviceCodes(login);
                if (accessibleDeviceCodes.Count == 0)
                {
                    return Ok(new List<DeviceLog>());
                }

                var latestDates = await tocontext.Logs // Получаем последние datetime для каждого устройства
                    .Where(l => accessibleDeviceCodes.Contains(l.DeviceCode))
                    .GroupBy(l => l.DeviceCode)
                    .Select(g => new
                    {
                        DeviceCode = g.Key,
                        LatestDatetime = g.Max(l => l.Datetime) // Последнее время
                    })
                    .ToListAsync();

                if (latestDates.Count == 0)
                {
                    return Ok(new List<DeviceLog>());
                }

                var deviceCodes = latestDates.Select(ld => ld.DeviceCode).ToList(); // Получаем коды устройств
                var allLogs = await tocontext.Logs // Получаем все логи для этих устройств одним запросом
                    .Where(l => deviceCodes.Contains(l.DeviceCode))
                    .ToListAsync();

                var devices = await tocontext.Devices // Получаем устройства одним запросом
                    .Where(d => deviceCodes.Contains(d.DeviceCode))
                    .ToListAsync();
                
                var deviceDict = devices.ToDictionary(d => d.DeviceCode); // Словарь устройств

                var actions = await tocontext.Actions.ToListAsync(); // Получаем все действия одним запросом
                var actionDict = actions.ToDictionary(
                    a => $"{a.ActionCode}|{a.AppVersion}",
                    a => a.ActionText
                );

                var deviceLogsList = new List<DeviceLog>(); // Список логов для устройств
                
                foreach (var latest in latestDates)
                {
                    var log = allLogs.FirstOrDefault(l =>  // Находим лог с точным совпадением device_code и datetime
                        l.DeviceCode == latest.DeviceCode && 
                        l.Datetime == latest.LatestDatetime);
                    
                    if (log == null) continue;

                    deviceDict.TryGetValue(log.DeviceCode, out var device); // Получаем device из словаря        
                    var actionKey = $"{log.ActionCode}|{log.AppVersion}"; // Получаем action text из словаря
                    actionDict.TryGetValue(actionKey, out var actionText);

                    deviceLogsList.Add(new DeviceLog // Добавляем лог в список
                    {
                        ActionCode = log.ActionCode,
                        AppVersion = log.AppVersion,
                        DeviceCode = log.DeviceCode,
                        Datetime = log.Datetime,
                        RegionCode = device?.RegionCode ?? "-",
                        SMPCode = device?.SmpCode ?? "-",
                        TeamNumber = log?.TeamNumber ?? "-",
                        ActionText = actionText ?? "Неизвестное действие"
                    });
                }

                return Ok(deviceLogsList.OrderByDescending(dl => dl.Datetime));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при получении логов: {ex.Message}" });
            }
        }


        [HttpGet("device/{deviceCode}")] // Логи для устройства
        public async Task<IActionResult> GetDeviceLogs(string deviceCode, 
            [FromQuery] string[]? action_text,
            [FromQuery] string[]? app_version,
            [FromQuery] string[]? region_code,
            [FromQuery] string[]? smp_code,
            [FromQuery] string[]? team_number)
        {
            if (string.IsNullOrEmpty(deviceCode))
            {
                return BadRequest(new { error = "Не указан код устройства" });
            }

            try
            {
                var device = await tocontext.Devices // Получаем устройство
                    .Where(d => d.DeviceCode == deviceCode)
                    .Select(d => new { d.RegionCode, d.SmpCode })
                    .FirstOrDefaultAsync();

                if (device == null)
                {
                    return NotFound(new { error = "Устройство не найдено" });
                }

                var login = HttpContext.Session.GetCurrentUser();
                if (!string.IsNullOrEmpty(login))
                {
                    var hasAccess = await tocontext.AdminSMPs
                        .AnyAsync(a => a.Login == login && 
                                      a.RegionCode == device.RegionCode && 
                                      a.SmpCode == device.SmpCode);

                    if (!hasAccess)
                    {
                        return StatusCode(403, new { error = "У вас нет доступа к этому устройству" });
                    }
                }

                var query = from log in tocontext.Logs // Основной запрос - объединяем таблицы
                           join d in tocontext.Devices on log.DeviceCode equals d.DeviceCode into deviceGroup
                           from d in deviceGroup.DefaultIfEmpty()
                           join a in tocontext.Actions on new { log.ActionCode, log.AppVersion } equals new { a.ActionCode, a.AppVersion } into actionGroup
                           from a in actionGroup.DefaultIfEmpty()
                           where log.DeviceCode == deviceCode
                           select new DeviceLog
                           {
                               ActionCode = log.ActionCode,
                               AppVersion = log.AppVersion,
                               DeviceCode = log.DeviceCode,
                               Datetime = log.Datetime,
                               RegionCode = d != null ? d.RegionCode : "-",
                               SMPCode = d != null ? d.SmpCode : "-",
                               TeamNumber = log.TeamNumber ?? "-",
                               ActionText = a != null ? a.ActionText : "Неизвестное действие"
                           };

                if (action_text != null && action_text.Length > 0) // Для фильтрации
                {
                    query = query.Where(dl => action_text.Contains(dl.ActionText));
                }

                if (app_version != null && app_version.Length > 0)
                {
                    query = query.Where(dl => app_version.Contains(dl.AppVersion));
                }

                if (region_code != null && region_code.Length > 0)
                {
                    query = query.Where(dl => region_code.Contains(dl.RegionCode));
                }

                if (smp_code != null && smp_code.Length > 0)
                {
                    query = query.Where(dl => smp_code.Contains(dl.SMPCode));
                }

                if (team_number != null && team_number.Length > 0)
                {
                    query = query.Where(dl => team_number.Contains(dl.TeamNumber));
                }

                var logs = await query.OrderByDescending(dl => dl.Datetime).ToListAsync();

                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при получении логов устройства: {ex.Message}" });
            }
        }
    }

    
}
