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

        [HttpGet("unique-values")] // Возвращает уникальные значения для фильтров из последних логов каждого устройства с учетом примененных фильтров
        public async Task<IActionResult> GetUniqueValues(
            [FromQuery] string[]? region_code = null, // Фильтр по региону
            [FromQuery] string[]? smp_code = null, // Фильтр по СМП
            [FromQuery] string[]? team_number = null, // Фильтр по команде
            [FromQuery] string[]? action_text = null, // Фильтр по действию
            [FromQuery] string[]? app_version = null, // Фильтр по версии приложения
            [FromQuery] string[]? device_code = null) // Фильтр по коду устройства
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

                // Получаем последние datetime для каждого устройства
                var latestDates = await tocontext.Logs
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
                    return Ok(new { });
                }

                var deviceCodes = latestDates.Select(ld => ld.DeviceCode).ToList();
                
                // Получаем все логи для этих устройств
                var allLogs = await tocontext.Logs
                    .Where(l => deviceCodes.Contains(l.DeviceCode))
                    .ToListAsync();

                // Получаем устройства
                var devices = await tocontext.Devices
                    .Where(d => deviceCodes.Contains(d.DeviceCode))
                    .ToListAsync();
                
                var deviceDict = devices.ToDictionary(d => d.DeviceCode);

                // Получаем действия
                var actions = await tocontext.Actions.ToListAsync();
                var actionDict = actions.ToDictionary(
                    a => $"{a.ActionCode}|{a.AppVersion}",
                    a => a.ActionText
                );

                // Собираем последние логи для каждого устройства
                var latestLogs = new List<DeviceLog>();
                
                foreach (var latest in latestDates)
                {
                    var log = allLogs.FirstOrDefault(l =>
                        l.DeviceCode == latest.DeviceCode && 
                        l.Datetime == latest.LatestDatetime);
                    
                    if (log == null) continue;

                    deviceDict.TryGetValue(log.DeviceCode, out var device);
                    var actionKey = $"{log.ActionCode}|{log.AppVersion}";
                    actionDict.TryGetValue(actionKey, out var actionText);

                    latestLogs.Add(new DeviceLog
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

                // Применяем фильтры к последним логам перед получением уникальных значений
                var filteredLatestLogs = latestLogs.AsQueryable();

                if (region_code != null && region_code.Length > 0)
                {
                    filteredLatestLogs = filteredLatestLogs.Where(dl => region_code.Contains(dl.RegionCode));
                }

                if (smp_code != null && smp_code.Length > 0)
                {
                    filteredLatestLogs = filteredLatestLogs.Where(dl => smp_code.Contains(dl.SMPCode));
                }

                if (team_number != null && team_number.Length > 0)
                {
                    filteredLatestLogs = filteredLatestLogs.Where(dl => team_number.Contains(dl.TeamNumber));
                }

                if (action_text != null && action_text.Length > 0)
                {
                    filteredLatestLogs = filteredLatestLogs.Where(dl => action_text.Contains(dl.ActionText));
                }

                if (app_version != null && app_version.Length > 0)
                {
                    filteredLatestLogs = filteredLatestLogs.Where(dl => app_version.Contains(dl.AppVersion));
                }

                if (device_code != null && device_code.Length > 0)
                {
                    filteredLatestLogs = filteredLatestLogs.Where(dl => device_code.Contains(dl.DeviceCode));
                }

                var filteredLogsList = filteredLatestLogs.ToList();

                // Теперь берем уникальные значения из ОТФИЛЬТРОВАННЫХ последних логов
                var regions = filteredLogsList
                    .Where(dl => !string.IsNullOrEmpty(dl.RegionCode) && dl.RegionCode != "-")
                    .Select(dl => dl.RegionCode)
                    .Distinct()
                    .OrderBy(r => r)
                    .ToList();

                var smpCodes = filteredLogsList
                    .Where(dl => !string.IsNullOrEmpty(dl.SMPCode) && dl.SMPCode != "-")
                    .Select(dl => dl.SMPCode)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();

                var teamNumbers = filteredLogsList
                    .Where(dl => !string.IsNullOrEmpty(dl.TeamNumber) && dl.TeamNumber != "-")
                    .Select(dl => dl.TeamNumber)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();

                var appVersions = filteredLogsList
                    .Where(dl => !string.IsNullOrEmpty(dl.AppVersion))
                    .Select(dl => dl.AppVersion)
                    .Distinct()
                    .OrderBy(v => v)
                    .ToList();

                var actionTexts = filteredLogsList
                    .Where(dl => !string.IsNullOrEmpty(dl.ActionText) && dl.ActionText != "Неизвестное действие")
                    .Select(dl => dl.ActionText)
                    .Distinct()
                    .OrderBy(a => a)
                    .ToList();

                var deviceCodesUnique = filteredLogsList
                    .Where(dl => !string.IsNullOrEmpty(dl.DeviceCode))
                    .Select(dl => dl.DeviceCode)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                return Ok(new // Возвращаем объект с уникальными значениями из ПОСЛЕДНИХ логов
                {
                    region_code = regions,
                    smp_code = smpCodes,
                    team_number = teamNumbers,
                    app_version = appVersions,
                    action_text = actionTexts,
                    device_code = deviceCodesUnique
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

        [HttpGet("latest")] // Последние логи для каждого устройства, доступного текущему админу с пагинацией
        public async Task<IActionResult> GetLatestDeviceLogs(
            [FromQuery] int offset = 0, // Смещение для пагинации (с какого элемента начинать)
            [FromQuery] int limit = 50, // Количество элементов на странице
            [FromQuery] string[]? region_code = null, // Фильтр по региону
            [FromQuery] string[]? smp_code = null, // Фильтр по СМП
            [FromQuery] string[]? team_number = null, // Фильтр по команде
            [FromQuery] string[]? action_text = null, // Фильтр по действию
            [FromQuery] string[]? app_version = null, // Фильтр по версии приложения
            [FromQuery] string[]? device_code = null) // Фильтр по коду устройства
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
                    return Ok(new { data = new List<DeviceLog>(), total = 0, hasMore = false });
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
                    return Ok(new { data = new List<DeviceLog>(), total = 0, hasMore = false });
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

                // Фильтры для оптимизации
                var filteredLogs = deviceLogsList.AsQueryable();

                if (region_code != null && region_code.Length > 0)
                {
                    filteredLogs = filteredLogs.Where(dl => region_code.Contains(dl.RegionCode));
                }

                if (smp_code != null && smp_code.Length > 0)
                {
                    filteredLogs = filteredLogs.Where(dl => smp_code.Contains(dl.SMPCode));
                }

                if (team_number != null && team_number.Length > 0)
                {
                    filteredLogs = filteredLogs.Where(dl => team_number.Contains(dl.TeamNumber));
                }

                if (action_text != null && action_text.Length > 0)
                {
                    filteredLogs = filteredLogs.Where(dl => action_text.Contains(dl.ActionText));
                }

                if (app_version != null && app_version.Length > 0)
                {
                    filteredLogs = filteredLogs.Where(dl => app_version.Contains(dl.AppVersion));
                }

                if (device_code != null && device_code.Length > 0)
                {
                    filteredLogs = filteredLogs.Where(dl => device_code.Contains(dl.DeviceCode));
                }

                // Сортируем по дате (по убыванию) перед пагинацией
                var orderedLogs = filteredLogs.OrderByDescending(dl => dl.Datetime);
                
                var totalCount = orderedLogs.Count(); // Общее количество после фильтрации
                var totalUnfiltered = deviceLogsList.Count; // Общее количество БЕЗ фильтров
                
                // Применяем пагинацию - пропускаем offset элементов и берем limit элементов
                var paginatedLogs = orderedLogs
                    .Skip(offset)
                    .Take(limit)
                    .ToList();

                var hasMore = (offset + limit) < totalCount; // Есть ли еще элементы для загрузки

                return Ok(new 
                { 
                    data = paginatedLogs, 
                    total = totalCount, 
                    totalUnfiltered = totalUnfiltered, // Общее количество без фильтров
                    hasMore = hasMore 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при получении логов: {ex.Message}" });
            }
        }


        [HttpGet("device/{deviceCode}/unique-values")] // Уникальные значения для фильтров конкретного устройства с учетом примененных фильтров
        public async Task<IActionResult> GetDeviceUniqueValues(
            string deviceCode,
            [FromQuery] string[]? team_number = null, // Фильтр по команде
            [FromQuery] string[]? action_text = null, // Фильтр по действию
            [FromQuery] string[]? app_version = null) // Фильтр по версии приложения
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

                var regionCode = device.RegionCode;
                var smpCode = device.SmpCode;

                // Получаем все логи устройства
                var query = from log in tocontext.Logs
                           join action in tocontext.Actions on new { log.ActionCode, log.AppVersion } equals new { action.ActionCode, action.AppVersion }
                           where log.DeviceCode == deviceCode
                           select new
                           {
                               log.TeamNumber,
                               log.AppVersion,
                               action.ActionText
                           };

                // Применяем фильтры
                if (team_number != null && team_number.Length > 0)
                {
                    query = query.Where(l => team_number.Contains(l.TeamNumber));
                }

                if (action_text != null && action_text.Length > 0)
                {
                    query = query.Where(l => action_text.Contains(l.ActionText));
                }

                if (app_version != null && app_version.Length > 0)
                {
                    query = query.Where(l => app_version.Contains(l.AppVersion));
                }

                var filteredLogs = await query.ToListAsync();

                // Получаем уникальные значения из отфильтрованных логов
                var teamNumbers = filteredLogs
                    .Where(l => !string.IsNullOrEmpty(l.TeamNumber))
                    .Select(l => l.TeamNumber)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();

                var appVersions = filteredLogs
                    .Where(l => !string.IsNullOrEmpty(l.AppVersion))
                    .Select(l => l.AppVersion)
                    .Distinct()
                    .OrderBy(v => v)
                    .ToList();

                var actionTexts = filteredLogs
                    .Where(l => !string.IsNullOrEmpty(l.ActionText))
                    .Select(l => l.ActionText)
                    .Distinct()
                    .OrderBy(a => a)
                    .ToList();

                return Ok(new // Возвращаем объект с уникальными значениями
                {
                    region_code = new[] { regionCode },
                    smp_code = new[] { smpCode },
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

        [HttpGet("device/{deviceCode}")] // Логи для устройства с пагинацией
        public async Task<IActionResult> GetDeviceLogs(string deviceCode,
            [FromQuery] int offset = 0, // Смещение для пагинации
            [FromQuery] int limit = 50, // Количество элементов на странице
            [FromQuery] string[]? action_text = null, // Фильтр по действию
            [FromQuery] string[]? app_version = null, // Фильтр по версии
            [FromQuery] string[]? region_code = null, // Фильтр по региону
            [FromQuery] string[]? smp_code = null, // Фильтр по СМП
            [FromQuery] string[]? team_number = null) // Фильтр по команде
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

                // Применяем сортировку
                var orderedQuery = query.OrderByDescending(dl => dl.Datetime);
                
                var totalCount = await orderedQuery.CountAsync(); // Общее количество после фильтрации
                
                // Получаем общее количество БЕЗ фильтров (только для этого устройства)
                var totalUnfiltered = await (from log in tocontext.Logs
                                             where log.DeviceCode == deviceCode
                                             select log).CountAsync();
                
                // Применяем пагинацию - пропускаем offset элементов и берем limit элементов
                var paginatedLogs = await orderedQuery
                    .Skip(offset)
                    .Take(limit)
                    .ToListAsync();

                var hasMore = (offset + limit) < totalCount; // Есть ли еще элементы

                return Ok(new 
                { 
                    data = paginatedLogs, 
                    total = totalCount,
                    totalUnfiltered = totalUnfiltered, // Общее количество без фильтров
                    hasMore = hasMore 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при получении логов устройства: {ex.Message}" });
            }
        }
    }

    
}
