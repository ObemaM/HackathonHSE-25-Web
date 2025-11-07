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
        private readonly ApplicationDbContext tocontext;

        public ActionsController(ApplicationDbContext context)
        {
            tocontext = context;
        }

        // GetAllActions возвращает все действия
        [HttpGet]
        public async Task<IActionResult> GetAllActions()
        {
            try
            {
                var actions = await tocontext.Actions.ToListAsync();
                return Ok(actions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при получении действий: {ex.Message}" });
            }
        }
    }
}
