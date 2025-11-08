using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HackathonBackend.Data;
using HackathonBackend.Models;
using HackathonBackend.Utils;
using static HackathonBackend.Utils.LINQBuilder;

namespace HackathonBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly ApplicationDbContext tocontext;

        public DevicesController(ApplicationDbContext context)
        {
            tocontext = context;
        }

        private IQueryable<Device> ApplyAdminFilters(IQueryable<Device> query) // Фильтры для админов
        {
            var login = HttpContext.Session.GetCurrentUser(); 
            if (string.IsNullOrEmpty(login))
            {
                return query.Where(d => false); // Пустой список, если не авторизирован
            }

            var permissions = tocontext.AdminSMPs // Получаем права доступа админа (по региону и смп)
                .Where(a => a.Login == login)
                .Select(p => new { p.RegionCode, p.SmpCode })
                .ToList();

            if (permissions.Count == 0)
            {
                return query.Where(d => false); // Пустой список, если нет прав доступа
            }

            var predicate = False<Device>(); // Создаем OR условия для каждой пары регион-СМП
            foreach (var perm in permissions)
            {
                var regionCode = perm.RegionCode;
                var smpCode = perm.SmpCode;
                predicate = predicate.Or(d => d.RegionCode == regionCode && d.SmpCode == smpCode);
            }

            return query.Where(predicate);
        }

        [HttpGet] // Возвращает все устройства, доступные текущему админу (на основе его региона и смп)
        public async Task<IActionResult> GetAllDevices([FromQuery] string? region, [FromQuery] string? smp)
        {
            try
            {
                IQueryable<Device> query = tocontext.Devices;

                query = ApplyAdminFilters(query); // Применяем фильтры администратора

                if (!string.IsNullOrEmpty(region)) // Если указаны дополнительные параметры фильтрации, применяем их поверх прав доступа
                {
                    if (!string.IsNullOrEmpty(smp))
                    {
                        query = query.Where(d => d.RegionCode == region && d.SmpCode == smp); // Составной индекс idx_devices_region_smp
                    }
                    else
                    {
                        query = query.Where(d => d.RegionCode == region); // Используем первую часть составного индекса
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
