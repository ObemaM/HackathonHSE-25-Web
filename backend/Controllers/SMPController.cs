using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HackathonBackend.Data;

namespace HackathonBackend.Controllers
{
    /// <summary>
    /// SMPController обрабатывает запросы, связанные с СМП
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SMPController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SMPController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GetAllSMP возвращает все записи СМП
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllSMP()
        {
            try
            {
                var smps = await _context.SMPs.ToListAsync();
                return Ok(smps);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при получении СМП: {ex.Message}" });
            }
        }
    }
}
