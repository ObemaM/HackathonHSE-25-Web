using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HackathonBackend.Data;

namespace HackathonBackend.Controllers
{
    /// <summary>
    /// ActionController обрабатывает запросы, связанные с действиями
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ActionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ActionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GetAllActions возвращает все действия
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllActions()
        {
            try
            {
                var actions = await _context.Actions.ToListAsync();
                return Ok(actions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при получении действий: {ex.Message}" });
            }
        }
    }
}
