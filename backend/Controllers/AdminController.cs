using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HackathonBackend.Data;
using HackathonBackend.Models;
using HackathonBackend.Models.DTOs;
using HackathonBackend.Utils;

namespace HackathonBackend.Controllers
{
    [ApiController]
    [Route("api")] // Обрабатывает запросы связанные с админами
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext tocontext;
        private readonly ILogger<AdminController> tologger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            tocontext = context;
            tologger = logger;
        }

        [HttpGet("admins")] // Возвращает всех админов
        public async Task<IActionResult> GetAllAdmins()
        {
            try
            {
                var admins = await tocontext.Admins.ToListAsync(); // Асинхронные функции, чтобы не блокировать поток все время
                return Ok(admins);  
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при получении администраторов: {ex.Message}" });
            }
        }

        [HttpGet("admins-smp")] // Возвращает список связей между админами и СМП
        public async Task<IActionResult> GetAllAdminSMP()
        {
            try
            {
                var adminSMPs = await tocontext.AdminSMPs.ToListAsync();
                return Ok(adminSMPs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при получении связей администраторов и СМП: {ex.Message}" });
            }
        }

        [HttpPost("login")] // Обрабатывает вход администратора
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Неверный формат запроса" });
            }

            try
            {
                var admin = await tocontext.Admins.FirstOrDefaultAsync(a => a.Login == request.Login);
                            
                if (admin == null) // Проверка пароля
                {
                    return Unauthorized(new { error = "Неверный логин или пароль" });
                }
                if (!PasswordHelper.CheckPasswordHash(request.Password, admin.PasswordHash))
                {
                    return Unauthorized(new { error = "Неверный логин или пароль" });
                }

                HttpContext.Session.SetUserSession(admin.Login); // Устанавливаем сессию пользователя, чтобы не приходилось постоянно логаться

                return Ok(new
                {
                    message = "Успешный вход",
                    login = admin.Login
                });
            }
            catch (Exception ex)
            {
                tologger.LogError($"Ошибка при входе: {ex.Message}");
                return StatusCode(500, new { error = "Ошибка при входе в систему" });
            }
        }

        [HttpPost("logout")] // Обработка выхода администратора
        public IActionResult Logout()
        {
            try
            {
                HttpContext.Session.ClearUserSession();
                return Ok(new { message = "Успешный выход" });
            }
            catch (Exception ex)
            {
                tologger.LogError($"Ошибка при выходе: {ex.Message}");
                return StatusCode(500, new { error = "Ошибка при выходе из системы" });
            }
        }

        [HttpGet("me")] // Возвращает информацию о текущем пользователе
        public async Task<IActionResult> GetCurrentAdmin()
        {
            var login = HttpContext.Session.GetCurrentUser();
            if (string.IsNullOrEmpty(login))
            {
                return Unauthorized(new { error = "Пользователь не аутентифицирован" });
            }

            try
            {
                var admin = await tocontext.Admins.FirstOrDefaultAsync(a => a.Login == login);
                
                if (admin == null)
                {
                    return StatusCode(500, new { error = "Ошибка при получении данных пользователя" });
                }

                admin.PasswordHash = ""; // Не возвращаем хеш пароля
                return Ok(admin);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при получении данных пользователя: {ex.Message}" });
            }
        }

        [HttpGet("me/smps")] // Возвращает список смп пользователя
        public async Task<IActionResult> GetCurrentUserSMPs()
        {
            var login = HttpContext.Session.GetCurrentUser();
            if (string.IsNullOrEmpty(login))
            {
                return Unauthorized(new { error = "Пользователь не аутентифицирован" });
            }

            try
            {
                var smps = await tocontext.AdminSMPs
                    .Where(asm => asm.Login == login)
                    .Join(tocontext.SMPs,
                        asm => new { asm.RegionCode, asm.SmpCode },
                        smp => new { smp.RegionCode, smp.SmpCode },
                        (asm, smp) => smp)
                    .ToListAsync();

                return Ok(smps);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при получении списка СМП: {ex.Message}" });
            }
        }
    }
}