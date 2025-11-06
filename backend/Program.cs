using Microsoft.EntityFrameworkCore;
using HackathonBackend.Data;
using HackathonBackend.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Конфигурация подключения к базе данных
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Если переменные окружения заданы, используем их
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5434";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "HackathonHSE-25";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "040506";

// Формируем строку подключения
connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};SSL Mode=Disable";

// Добавляем DbContext с PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Добавляем контроллеры
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Настройка JSON сериализации
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Настройка сессий
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = builder.Configuration["Session:CookieName"] ?? "session";
    options.IdleTimeout = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("Session:IdleTimeout", 10080)); // 7 дней по умолчанию
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Настройка CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" };
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .WithExposedHeaders("Content-Length");
    });
});

// Добавляем Swagger для документации API (опционально)
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Проверяем подключение к БД при запуске
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        await context.Database.CanConnectAsync();
        Console.WriteLine("Успешное подключение к БД");
        
        // Применяем миграции автоматически
        await context.Database.MigrateAsync();
        Console.WriteLine("Миграции применены");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Не удалось подключиться к БД: {ex.Message}");
        throw;
    }
}

// Настройка middleware pipeline
app.UseSession();
app.UseCors();

// Redirect root to API documentation
app.MapGet("/", () => Results.Redirect("/api"));

// Redirect monitoring to frontend
app.MapGet("/monitoring", () => Results.Redirect("http://localhost:3000"));

// API documentation endpoint
app.MapGet("/api", () => Results.Json(new
{
    message = "Это API проекта",
    endpoints = new
    {
        smp = "/api/smp",
        admins = "/api/admins",
        admins_smp = "/api/admins-smp",
        actions = "/api/actions",
        devices = "/api/devices",
        logs = "/api/logs",
        health = "/health",
        monitoring = "/monitoring"
    }
}));

// Test endpoint
app.MapGet("/api/test", () => Results.Json(new
{
    message = "Test endpoint is working",
    status = "success"
}));

// Публичные маршруты (без аутентификации)
app.MapControllers();

// Защищенные маршруты (требуют аутентификации)
// Применяем middleware аутентификации только к определенным маршрутам
app.MapWhen(
    context => context.Request.Path.StartsWithSegments("/api") &&
               !context.Request.Path.StartsWithSegments("/api/login") &&
               !context.Request.Path.StartsWithSegments("/api/test"),
    appBuilder =>
    {
        appBuilder.UseAuthenticationMiddleware();
        appBuilder.UseRouting();
        appBuilder.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    });

Console.WriteLine("Запущено на http://localhost:8080");
app.Run("http://0.0.0.0:8080");
