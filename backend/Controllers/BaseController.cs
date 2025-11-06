using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HackathonBackend.Data;

namespace HackathonBackend.Controllers
{
    /// <summary>
    /// BaseController содержит общие методы для контроллеров
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class BaseController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BaseController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// HealthCheck обрабатывает запрос на проверку работоспособности
        /// </summary>
        [HttpGet("/health")]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                // Проверяем соединение с БД
                await _context.Database.CanConnectAsync();

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
