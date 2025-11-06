using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HackathonBackend.Data;
using HackathonBackend.Models;
using HackathonBackend.Models.DTOs;
using HackathonBackend.Utils;

namespace HackathonBackend.Controllers
{
    /// <summary>
    /// AdminController обрабатывает запросы, связанные с администраторами
    /// </summary>
    [ApiController]
    [Route("api")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// GetAllAdmins возвращает всех администраторов
        /// </summary>
        [HttpGet("admins")]
        public async Task<IActionResult> GetAllAdmins()
        {
            try
            {
                var admins = await _context.Admins.ToListAsync();
                return Ok(admins);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при получении администраторов: {ex.Message}" });
            }
        }

        /// <summary>
        /// GetAllAdminSMP возвращает все связи между администраторами и СМП
        /// </summary>
        [HttpGet("admins-smp")]
        public async Task<IActionResult> GetAllAdminSMP()
        {
            try
            {
                var adminSMPs = await _context.AdminSMPs.ToListAsync();
                return Ok(adminSMPs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при получении связей администраторов и СМП: {ex.Message}" });
            }
        }

        /// <summary>
        /// Login обрабатывает вход администратора
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Неверный формат запроса" });
            }

            try
            {
                var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Login == request.Login);
                
                if (admin == null)
                {
                    // Use the same error message to prevent user enumeration
                    return Unauthorized(new { error = "Неверный логин или пароль" });
                }

                // Verify password using bcrypt
                if (!PasswordHelper.CheckPasswordHash(request.Password, admin.PasswordHash))
                {
                    return Unauthorized(new { error = "Неверный логин или пароль" });
                }

                // Устанавливаем сессию пользователя
                HttpContext.Session.SetUserSession(admin.Login);

                return Ok(new
                {
                    message = "Успешный вход",
                    login = admin.Login
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при входе: {ex.Message}");
                return StatusCode(500, new { error = "Ошибка при входе в систему" });
            }
        }

        /// <summary>
        /// Logout обрабатывает выход администратора
        /// </summary>
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            try
            {
                HttpContext.Session.ClearUserSession();
                return Ok(new { message = "Успешный выход" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при выходе: {ex.Message}");
                return StatusCode(500, new { error = "Ошибка при выходе из системы" });
            }
        }

        /// <summary>
        /// GetCurrentAdmin возвращает информацию о текущем аутентифицированном пользователе
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentAdmin()
        {
            var login = HttpContext.Session.GetCurrentUser();
            if (string.IsNullOrEmpty(login))
            {
                return Unauthorized(new { error = "Пользователь не аутентифицирован" });
            }

            try
            {
                var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Login == login);
                
                if (admin == null)
                {
                    return StatusCode(500, new { error = "Ошибка при получении данных пользователя" });
                }

                // Не возвращаем хеш пароля
                admin.PasswordHash = "";
                return Ok(admin);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при получении данных пользователя: {ex.Message}" });
            }
        }

        /// <summary>
        /// GetCurrentUserSMPs возвращает список СМП текущего пользователя
        /// </summary>
        [HttpGet("me/smps")]
        public async Task<IActionResult> GetCurrentUserSMPs()
        {
            var login = HttpContext.Session.GetCurrentUser();
            if (string.IsNullOrEmpty(login))
            {
                return Unauthorized(new { error = "Пользователь не аутентифицирован" });
            }

            try
            {
                var smps = await _context.AdminSMPs
                    .Where(asm => asm.Login == login)
                    .Join(_context.SMPs,
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
