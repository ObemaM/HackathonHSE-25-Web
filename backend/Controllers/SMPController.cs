using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HackathonBackend.Data;

namespace HackathonBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Все запросы с мобильных устройств
    public class SMPController : ControllerBase
    {
        private readonly ApplicationDbContext tocontext;

        public SMPController(ApplicationDbContext context)
        {
            tocontext = context;
        }

        [HttpGet] // Получает все записи СМП
        public async Task<IActionResult> GetAllSMP()
        {
            try
            {
                var smps = await tocontext.SMPs.ToListAsync();
                return Ok(smps);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при получении СМП: {ex.Message}" });
            }
        }
    }
}
