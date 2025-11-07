using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HackathonBackend.Data;
using HackathonBackend.Models;
using HackathonBackend.Utils;
using static HackathonBackend.Utils.LINQBuilder;

namespace HackathonBackend.Controllers
{
    /// <summary>
    /// DeviceController обрабатывает запросы, связанные с устройствами
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DevicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Применяет фильтры администратора к запросу устройств
        /// </summary>
        private IQueryable<Device> ApplyAdminFilters(IQueryable<Device> query)
        {
            // Get current admin
            var login = HttpContext.Session.GetCurrentUser();
            if (string.IsNullOrEmpty(login))
            {
                // If not logged in, return no results
                return query.Where(d => false);
            }

            // Get admin permissions - материализуем список
            var permissions = _context.AdminSMPs
                .Where(a => a.Login == login)
                .Select(p => new { p.RegionCode, p.SmpCode })
                .ToList();

            if (permissions.Count == 0)
            {
                // If no specific permissions, return empty result
                return query.Where(d => false);
            }

            // Создаем OR условия для каждой пары регион-СМП
            var predicate = False<Device>();
            foreach (var perm in permissions)
            {
                var regionCode = perm.RegionCode;
                var smpCode = perm.SmpCode;
                predicate = predicate.Or(d => d.RegionCode == regionCode && d.SmpCode == smpCode);
            }

            return query.Where(predicate);
        }

        /// <summary>
        /// GetAllDevices возвращает все устройства, доступные текущему администратору
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllDevices([FromQuery] string? region, [FromQuery] string? smp)
        {
            try
            {
                IQueryable<Device> query = _context.Devices;

                // Применяем фильтры администратора
                query = ApplyAdminFilters(query);

                // Если указаны дополнительные параметры фильтрации, применяем их поверх прав доступа
                if (!string.IsNullOrEmpty(region))
                {
                    if (!string.IsNullOrEmpty(smp))
                    {
                        // Используем составной индекс idx_devices_region_smp
                        query = query.Where(d => d.RegionCode == region && d.SmpCode == smp);
                    }
                    else
                    {
                        // Используем первую часть составного индекса
                        query = query.Where(d => d.RegionCode == region);
                    }
                }

                var devices = await query.ToListAsync();
                return Ok(devices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при получении устройств: {ex.Message}" });
            }
        }
    }
}
