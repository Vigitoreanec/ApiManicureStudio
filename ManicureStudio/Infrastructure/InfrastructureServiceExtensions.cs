using ManicureStudio.Core.Interfaces;
using ManicureStudio.Infrastructure.Data;
using ManicureStudio.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ManicureStudio.Infrastructure
{
    public static class InfrastructureServiceExtensions
    {
        public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
        {
            // ──────────────── Подключение к SQL Server ────────────────

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Строка подключения 'DefaultConnection' не найдена в конфигурации. " +
                    "Проверьте appsettings.json или переменные окружения.");

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    // Автоматическая повторная попытка при временных сбоях БД
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,               // Максимум 3 попытки
                        maxRetryDelay: TimeSpan.FromSeconds(5), // Ждём 5 сек между попытками
                        errorNumbersToAdd: null          // Стандартный набор ошибок для повтора
                    );

                    // Таймаут команды — 30 секунд
                    sqlOptions.CommandTimeout(30);
                });

                // В режиме разработки: подробные логи SQL-запросов
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    options.EnableSensitiveDataLogging();  // Показывает значения параметров в логах
                    options.EnableDetailedErrors();         // Подробные сообщения об ошибках
                }
            });

            // ──────────────── Регистрация репозиториев ────────────────
            // Scoped = один экземпляр на HTTP-запрос (правильно для работы с БД)

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>)); // Универсальный репозиторий
            services.AddScoped<IClientRepository, ClientRepository>();
            services.AddScoped<IMasterRepository, MasterRepository>();
            services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            services.AddScoped<IServiceRepository, ServiceRepository>();

            return services;
        }
    }
}
