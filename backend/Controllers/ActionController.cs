using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HackathonBackend.Data;

namespace HackathonBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // ActionController обрабатывает запросы действий
    public class ActionsController : ControllerBase
    {
        private readonly ApplicationDbContext tocontext;

        public ActionsController(ApplicationDbContext context)
        {
            tocontext = context;
        }
        
        [HttpGet] // GetAllActions возвращает все действия
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