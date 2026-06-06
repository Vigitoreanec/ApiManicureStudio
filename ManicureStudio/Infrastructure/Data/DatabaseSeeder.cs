using ManicureStudio.Core.Entities;
using ManicureStudio.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ManicureStudio.Infrastructure.Data;

/// <summary>
/// Сидер (наполнитель) базы данных начальными данными.
/// Запускается при старте приложения и заполняет БД тестовыми/базовыми данными.
/// Безопасен для повторного запуска — не дублирует данные.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Главный метод сидирования.
    /// Вызывается из Program.cs при запуске.
    /// </summary>
    public static async Task SeedAsync(AppDbContext context, ILogger logger)
    {
        try
        {
            // Применяем все ожидающие миграции автоматически
            await context.Database.MigrateAsync();
            logger.LogInformation("✅ Миграции БД применены успешно");

            // Заполняем данные в правильном порядке (учитывая зависимости)
            await SeedCategoriesAsync(context, logger);
            await SeedServicesAsync(context, logger);
            await SeedMastersAsync(context, logger);
            await SeedMasterServicesAsync(context, logger);

            logger.LogInformation("✅ База данных успешно инициализирована");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Ошибка при инициализации базы данных");
            throw;
        }
    }

    // ──────────────── Категории услуг ────────────────

    private static async Task SeedCategoriesAsync(AppDbContext context, ILogger logger)
    {
        // Проверяем: уже есть категории? Тогда пропускаем
        if (await context.ServiceCategories.AnyAsync()) return;

        var categories = new List<ServiceCategory>
        {
            new() { Name = "Маникюр",     Description = "Уход за ногтями рук", SortOrder = 1 },
            new() { Name = "Педикюр",     Description = "Уход за ногтями ног", SortOrder = 2 },
            new() { Name = "Наращивание", Description = "Наращивание и коррекция ногтей", SortOrder = 3 },
            new() { Name = "Дизайн",      Description = "Художественное оформление ногтей", SortOrder = 4 },
            new() { Name = "SPA-уход",    Description = "Восстанавливающие процедуры для рук и ног", SortOrder = 5 },
        };

        await context.ServiceCategories.AddRangeAsync(categories);
        await context.SaveChangesAsync();

        logger.LogInformation("✅ Категории услуг добавлены: {Count}", categories.Count);
    }

    // ──────────────── Услуги ────────────────

    private static async Task SeedServicesAsync(AppDbContext context, ILogger logger)
    {
        if (await context.Services.AnyAsync()) return;

        // Получаем ID категорий
        var manicure  = await context.ServiceCategories.FirstAsync(c => c.Name == "Маникюр");
        var pedicure  = await context.ServiceCategories.FirstAsync(c => c.Name == "Педикюр");
        var extension = await context.ServiceCategories.FirstAsync(c => c.Name == "Наращивание");
        var design    = await context.ServiceCategories.FirstAsync(c => c.Name == "Дизайн");
        var spa       = await context.ServiceCategories.FirstAsync(c => c.Name == "SPA-уход");

        var services = new List<Service>
        {
            // Маникюр
            new() { Name = "Маникюр классический",  CategoryId = manicure.Id,  Price = 800,   DurationMinutes = 60  },
            new() { Name = "Маникюр аппаратный",     CategoryId = manicure.Id,  Price = 1000,  DurationMinutes = 75  },
            new() { Name = "Покрытие гель-лак",      CategoryId = manicure.Id,  Price = 1200,  DurationMinutes = 90  },
            new() { Name = "Снятие гель-лака",       CategoryId = manicure.Id,  Price = 400,   DurationMinutes = 30  },

            // Педикюр
            new() { Name = "Педикюр классический",  CategoryId = pedicure.Id,  Price = 1200,  DurationMinutes = 90  },
            new() { Name = "Педикюр аппаратный",    CategoryId = pedicure.Id,  Price = 1500,  DurationMinutes = 90  },
            new() { Name = "Педикюр + покрытие",    CategoryId = pedicure.Id,  Price = 2000,  DurationMinutes = 120 },

            // Наращивание
            new() { Name = "Наращивание на типсы",  CategoryId = extension.Id, Price = 2500,  DurationMinutes = 150 },
            new() { Name = "Наращивание на формы",  CategoryId = extension.Id, Price = 2800,  DurationMinutes = 180 },
            new() { Name = "Коррекция наращенных",  CategoryId = extension.Id, Price = 1800,  DurationMinutes = 120 },

            // Дизайн
            new() { Name = "Дизайн простой (1 ноготь)", CategoryId = design.Id, Price = 100, DurationMinutes = 10  },
            new() { Name = "Дизайн сложный (1 ноготь)", CategoryId = design.Id, Price = 250, DurationMinutes = 20  },
            new() { Name = "Стемпинг",               CategoryId = design.Id, Price = 500,  DurationMinutes = 30  },

            // SPA
            new() { Name = "SPA маникюр",            CategoryId = spa.Id, Price = 1800,  DurationMinutes = 90  },
            new() { Name = "Парафинотерапия рук",    CategoryId = spa.Id, Price = 600,   DurationMinutes = 30  },
            new() { Name = "Японский маникюр P.Shine", CategoryId = spa.Id, Price = 2200, DurationMinutes = 90 },
        };

        await context.Services.AddRangeAsync(services);
        await context.SaveChangesAsync();

        logger.LogInformation("✅ Услуги добавлены: {Count}", services.Count);
    }

    // ──────────────── Мастера ────────────────

    private static async Task SeedMastersAsync(AppDbContext context, ILogger logger)
    {
        if (await context.Masters.AnyAsync()) return;

        var masters = new List<Master>
        {
            new()
            {
                FirstName     = "Анастасия",
                LastName      = "Иванова",
                PhoneNumber   = "+79001112233",
                Email         = "a.ivanova@manicure.studio",
                Specialization = "Маникюр, Дизайн, SPA-уход",
                Description    = "Мастер с 5-летним стажем. Специализируюсь на сложных дизайнах и SPA-процедурах.",
                
                IsActive      = true
            },
            new()
            {
                FirstName     = "Мария",
                LastName      = "Петрова",
                PhoneNumber   = "+79004445566",
                Email         = "m.petrova@manicure.studio",
                Specialization = "Маникюр, Педикюр, Наращивание",
                Description    = "Сертифицированный мастер, эксперт по наращиванию ногтей.",
                
                IsActive      = true
            },
            new()
            {
                FirstName     = "Екатерина",
                LastName      = "Сидорова",
                PhoneNumber   = "+79007778899",
                Email         = "e.sidorova@manicure.studio",
                Specialization = "Педикюр, SPA-уход",
                Description    = "Специалист по педикюру и восстановительным процедурам.",
                
                IsActive      = true
            }
        };

        await context.Masters.AddRangeAsync(masters);
        await context.SaveChangesAsync();

        logger.LogInformation("✅ Мастера добавлены: {Count}", masters.Count);
    }

    // ──────────────── Привязка мастеров к услугам ────────────────

    private static async Task SeedMasterServicesAsync(AppDbContext context, ILogger logger)
    {
        if (await context.MasterServices.AnyAsync()) return;

        var anastasia  = await context.Masters.FirstAsync(m => m.LastName == "Иванова");
        var maria      = await context.Masters.FirstAsync(m => m.LastName == "Петрова");
        var ekaterina  = await context.Masters.FirstAsync(m => m.LastName == "Сидорова");

        // Получаем ID услуг по названию
        var allServices = await context.Services.ToListAsync();
        int ServiceId(string name) => allServices.First(s => s.Name == name).Id;

        var masterServices = new List<MasterService>
        {
            // Анастасия: маникюр + дизайн + spa
            new() { MasterId = anastasia.Id, ServiceId = ServiceId("Маникюр классический") },
            new() { MasterId = anastasia.Id, ServiceId = ServiceId("Маникюр аппаратный") },
            new() { MasterId = anastasia.Id, ServiceId = ServiceId("Покрытие гель-лак") },
            new() { MasterId = anastasia.Id, ServiceId = ServiceId("Снятие гель-лака") },
            new() { MasterId = anastasia.Id, ServiceId = ServiceId("Дизайн простой (1 ноготь)") },
            new() { MasterId = anastasia.Id, ServiceId = ServiceId("Дизайн сложный (1 ноготь)") },
            new() { MasterId = anastasia.Id, ServiceId = ServiceId("Стемпинг") },
            new() { MasterId = anastasia.Id, ServiceId = ServiceId("SPA маникюр") },
            new() { MasterId = anastasia.Id, ServiceId = ServiceId("Парафинотерапия рук") },
            new() { MasterId = anastasia.Id, ServiceId = ServiceId("Японский маникюр P.Shine") },

            // Мария: маникюр + педикюр + наращивание
            new() { MasterId = maria.Id, ServiceId = ServiceId("Маникюр классический") },
            new() { MasterId = maria.Id, ServiceId = ServiceId("Покрытие гель-лак") },
            new() { MasterId = maria.Id, ServiceId = ServiceId("Педикюр классический") },
            new() { MasterId = maria.Id, ServiceId = ServiceId("Педикюр аппаратный") },
            new() { MasterId = maria.Id, ServiceId = ServiceId("Педикюр + покрытие") },
            new() { MasterId = maria.Id, ServiceId = ServiceId("Наращивание на типсы") },
            new() { MasterId = maria.Id, ServiceId = ServiceId("Наращивание на формы") },
            new() { MasterId = maria.Id, ServiceId = ServiceId("Коррекция наращенных") },

            // Екатерина: педикюр + spa
            new() { MasterId = ekaterina.Id, ServiceId = ServiceId("Педикюр классический") },
            new() { MasterId = ekaterina.Id, ServiceId = ServiceId("Педикюр аппаратный") },
            new() { MasterId = ekaterina.Id, ServiceId = ServiceId("Педикюр + покрытие") },
            new() { MasterId = ekaterina.Id, ServiceId = ServiceId("SPA маникюр") },
            new() { MasterId = ekaterina.Id, ServiceId = ServiceId("Парафинотерапия рук") },
        };

        await context.MasterServices.AddRangeAsync(masterServices);
        await context.SaveChangesAsync();

        logger.LogInformation("✅ Привязки мастеров к услугам добавлены: {Count}", masterServices.Count);
    }
}
