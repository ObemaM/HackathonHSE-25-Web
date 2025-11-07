using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HackathonBackend.Data;
using HackathonBackend.Models;
using HackathonBackend.Models.DTOs;
using HackathonBackend.Utils;
using static HackathonBackend.Utils.LINQBuilder;

namespace HackathonBackend.Controllers
{
    /// <summary>
    /// LogController обрабатывает запросы, связанные с логами
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получает список кодов устройств, доступных администратору
        /// </summary>
        private List<string> GetAccessibleDeviceCodes(string login)
        {
            // Получаем разрешения и материализуем их
            var permissions = _context.AdminSMPs
                .Where(a => a.Login == login)
                .Select(p => new { p.RegionCode, p.SmpCode })
                .ToList();

            if (permissions.Count == 0)
            {
                return new List<string>();
            }

            // Создаем OR условия для каждой пары регион-СМП
            var predicate = False<Device>();
            foreach (var perm in permissions)
            {
                var regionCode = perm.RegionCode;
                var smpCode = perm.SmpCode;
                predicate = predicate.Or(d => d.RegionCode == regionCode && d.SmpCode == smpCode);
            }

            var deviceCodes = _context.Devices
                .Where(predicate)
                .Select(d => d.DeviceCode)
                .ToList();

            return deviceCodes;
        }

        /// <summary>
        /// Применяет фильтры администратора к запросу логов
        /// </summary>
        private IQueryable<Log> ApplyAdminFilters(IQueryable<Log> query)
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

        /// <summary>
        /// GetUniqueValues returns unique values for filtering
        /// </summary>
        [HttpGet("unique-values")]
        public async Task<IActionResult> GetUniqueValues()
        {
            var login = HttpContext.Session.GetCurrentUser();
            if (string.IsNullOrEmpty(login))
            {
                return Unauthorized(new { error = "Требуется авторизация" });
            }

            try
            {
                var accessibleDeviceCodes = GetAccessibleDeviceCodes(login);
                if (accessibleDeviceCodes.Count == 0)
                {
                    return Ok(new { });
                }

                // Get unique region codes
                var regions = await _context.Devices
                    .Where(d => accessibleDeviceCodes.Contains(d.DeviceCode))
                    .Where(d => !string.IsNullOrEmpty(d.RegionCode))
                    .Select(d => d.RegionCode)
                    .Distinct()
                    .ToListAsync();

                // Get unique SMP codes
                var smpCodes = await _context.Devices
                    .Where(d => accessibleDeviceCodes.Contains(d.DeviceCode))
                    .Where(d => !string.IsNullOrEmpty(d.SmpCode))
                    .Select(d => d.SmpCode)
                    .Distinct()
                    .ToListAsync();

                // Get unique team numbers
                var teamNumbers = await _context.Logs
                    .Where(d => accessibleDeviceCodes.Contains(d.DeviceCode))
                    .Where(d => !string.IsNullOrEmpty(d.TeamNumber))
                    .Select(d => d.TeamNumber)
                    .Distinct()
                    .ToListAsync();

                // Get unique app versions
                var appVersions = await _context.Logs
                    .Where(l => accessibleDeviceCodes.Contains(l.DeviceCode))
                    .Where(l => !string.IsNullOrEmpty(l.AppVersion))
                    .Select(l => l.AppVersion)
                    .Distinct()
                    .ToListAsync();

                // Get unique action texts
                var actionTexts = await _context.Actions
                    .Where(a => !string.IsNullOrEmpty(a.ActionText))
                    .Select(a => a.ActionText)
                    .Distinct()
                    .ToListAsync();

                return Ok(new
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

        /// <summary>
        /// GetAllLogs возвращает все логи, доступные текущему администратору
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllLogs()
        {
            try
            {
                var query = _context.Logs.OrderByDescending(l => l.Datetime);
                var filteredQuery = ApplyAdminFilters(query);
                var logs = await filteredQuery.ToListAsync();
                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при получении логов: {ex.Message}" });
            }
        }

        /// <summary>
        /// GetLatestDeviceLogs возвращает последний лог для каждого устройства, доступного текущему администратору
        /// </summary>
        [HttpGet("latest")]
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

                // Шаг 1: Получаем последние datetime для каждого устройства
                var latestDates = await _context.Logs
                    .Where(l => accessibleDeviceCodes.Contains(l.DeviceCode))
                    .GroupBy(l => l.DeviceCode)
                    .Select(g => new
                    {
                        DeviceCode = g.Key,
                        LatestDatetime = g.Max(l => l.Datetime)
                    })
                    .ToListAsync();

                if (latestDates.Count == 0)
                {
                    return Ok(new List<DeviceLog>());
                }

                // Шаг 2: Получаем все логи для этих устройств одним запросом
                var deviceCodes = latestDates.Select(ld => ld.DeviceCode).ToList();
                var allLogs = await _context.Logs
                    .Where(l => deviceCodes.Contains(l.DeviceCode))
                    .ToListAsync();

                // Шаг 3: Получаем все devices одним запросом
                var devices = await _context.Devices
                    .Where(d => deviceCodes.Contains(d.DeviceCode))
                    .ToListAsync();
                
                var deviceDict = devices.ToDictionary(d => d.DeviceCode);

                // Шаг 4: Получаем все actions одним запросом
                var actions = await _context.Actions.ToListAsync();
                var actionDict = actions.ToDictionary(
                    a => $"{a.ActionCode}|{a.AppVersion}",
                    a => a.ActionText
                );

                // Шаг 5: Фильтруем последние логи в памяти и собираем результат
                var deviceLogsList = new List<DeviceLog>();
                
                foreach (var latest in latestDates)
                {
                    // Находим лог с точным совпадением device_code и datetime
                    var log = allLogs.FirstOrDefault(l => 
                        l.DeviceCode == latest.DeviceCode && 
                        l.Datetime == latest.LatestDatetime);
                    
                    if (log == null) continue;

                    // Получаем device из словаря
                    deviceDict.TryGetValue(log.DeviceCode, out var device);
                    
                    // Получаем action text из словаря
                    var actionKey = $"{log.ActionCode}|{log.AppVersion}";
                    actionDict.TryGetValue(actionKey, out var actionText);

                    deviceLogsList.Add(new DeviceLog
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

        

        /// <summary>
        /// GetDeviceLogs возвращает все логи для указанного устройства, если у администратора есть к нему доступ
        /// </summary>
        [HttpGet("device/{deviceCode}")]
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
                // First, verify the device exists and get its region and SMP code
                var device = await _context.Devices
                    .Where(d => d.DeviceCode == deviceCode)
                    .Select(d => new { d.RegionCode, d.SmpCode })
                    .FirstOrDefaultAsync();

                if (device == null)
                {
                    return NotFound(new { error = "Устройство не найдено" });
                }

                // Check if admin has access to this device's region and SMP
                var login = HttpContext.Session.GetCurrentUser();
                if (!string.IsNullOrEmpty(login))
                {
                    var hasAccess = await _context.AdminSMPs
                        .AnyAsync(a => a.Login == login && 
                                      a.RegionCode == device.RegionCode && 
                                      a.SmpCode == device.SmpCode);

                    if (!hasAccess)
                    {
                        return StatusCode(403, new { error = "У вас нет доступа к этому устройству" });
                    }
                }

                // Создаем базовый запрос
                var query = from log in _context.Logs
                           join d in _context.Devices on log.DeviceCode equals d.DeviceCode into deviceGroup
                           from d in deviceGroup.DefaultIfEmpty()
                           join a in _context.Actions on new { log.ActionCode, log.AppVersion } equals new { a.ActionCode, a.AppVersion } into actionGroup
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

                // Применяем фильтры
                if (action_text != null && action_text.Length > 0)
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
