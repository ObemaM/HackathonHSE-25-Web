using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HackathonBackend.Data;

namespace HackathonBackend.Controllers
{
    [ApiController]
    [Route("[controller]")] // Базовый контроллер
    public class BaseController : ControllerBase
    {
        private readonly ApplicationDbContext tocontext;

        public BaseController(ApplicationDbContext context)
        {
            tocontext = context;
        }

        [HttpGet("/health")] // Проверка работоспособности, просто возвращает UP если БД доступна
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                await tocontext.Database.CanConnectAsync(); // Проверяем соединение с БД

                return Ok(new
                {
                    status = "UP",
                    message = "БД доступна"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "DOWN",
                    message = $"БД не доступна: {ex.Message}" 
                });
            }
        }
    }
}
