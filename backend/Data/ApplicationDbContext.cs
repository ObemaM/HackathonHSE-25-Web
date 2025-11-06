using Microsoft.EntityFrameworkCore;
using HackathonBackend.Models;

namespace HackathonBackend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Admin> Admins { get; set; }
        public DbSet<SMP> SMPs { get; set; }
        public DbSet<AdminSMP> AdminSMPs { get; set; }
        public DbSet<Models.Action> Actions { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<Log> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Настройка составных первичных ключей
            modelBuilder.Entity<Models.Action>()
                .HasKey(a => new { a.ActionCode, a.AppVersion });

            modelBuilder.Entity<SMP>()
                .HasKey(s => new { s.RegionCode, s.SmpCode });

            modelBuilder.Entity<AdminSMP>()
                .HasKey(a => new { a.Login, a.RegionCode, a.SmpCode });

            modelBuilder.Entity<Log>()
                .HasKey(l => new { l.ActionCode, l.AppVersion, l.DeviceCode, l.Datetime });

            // Создание индексов для оптимизации запросов
            
            // Составной индекс для частого паттерна: WHERE device_code = ? ORDER BY datetime DESC
            modelBuilder.Entity<Log>()
                .HasIndex(l => new { l.DeviceCode, l.Datetime })
                .HasDatabaseName("idx_logs_device_datetime");

            // Составной индекс для фильтрации по региону и СМП в таблице devices
            modelBuilder.Entity<Device>()
                .HasIndex(d => new { d.RegionCode, d.SmpCode })
                .HasDatabaseName("idx_devices_region_smp");

            // Для поиска по командам
            modelBuilder.Entity<Device>()
                .HasIndex(d => d.TeamNumber)
                .HasDatabaseName("idx_devices_team");

            // Для поиска по версии приложения
            modelBuilder.Entity<Log>()
                .HasIndex(l => l.AppVersion)
                .HasDatabaseName("idx_logs_app_version");

            // Для поиска по коду действия
            modelBuilder.Entity<Log>()
                .HasIndex(l => l.ActionCode)
                .HasDatabaseName("idx_logs_action_code");

            // Составной индекс для таблицы admins_smp для быстрого поиска прав доступа
            modelBuilder.Entity<AdminSMP>()
                .HasIndex(a => new { a.Login, a.RegionCode, a.SmpCode })
                .HasDatabaseName("idx_admins_smp_login");

            // Индекс для поиска по логину администратора в таблице admins_smp
            modelBuilder.Entity<AdminSMP>()
                .HasIndex(a => a.Login)
                .HasDatabaseName("idx_admins_smp_login_only");

            // Настройка значений по умолчанию
            modelBuilder.Entity<Admin>()
                .Property(a => a.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<Log>()
                .Property(l => l.Datetime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
