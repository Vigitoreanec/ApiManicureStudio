using ManicureStudio.API;
using ManicureStudio.API.Middleware;
using ManicureStudio.Infrastructure;
using ManicureStudio.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

//------ Заменяем встроенный логгер
//builder.Host.UseSerilog(); CREATE

// Add services to the container.
//------ API
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = 
    System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});
//------ EF Core
builder.Services.AddInfrastructure(builder.Configuration);
//------ Swagger
builder.Services.AddSwaggerDocumentation(builder.Configuration);
//------ CORS политика
builder.Services.AddCorsPolicy(builder.Configuration);
//------ Health Checks — эндпоинт /health для мониторинга
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database"); // Проверяет подключение к БД

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
//------ Инициализация БД

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Применяем миграции и заполняем начальные данные
        await DatabaseSeeder.SeedAsync(context, logger);
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "? Критическая ошибка при инициализации БД. Приложение остановлено.");
        throw; 
    }
}

//------

// Глобальный обработчик исключений (ПЕРВЫМ в конвейере!)
app.UseMiddleware<ExceptionMiddleware>();
// Swagger только в разработке
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Manicure Studio API v1");
        c.RoutePrefix = "swagger";  // Доступен по /swagger
        c.DisplayRequestDuration(); // Показывает время ответа
    });
}

//------ HTTPS редирект
app.UseHttpsRedirection();

//------ CORS (до авторизации!)
app.UseCors("ManicureStudioCors");

//------ Аутентификация и авторизация
app.UseAuthentication();
app.UseAuthorization();

// Health check эндпоинт
app.MapHealthChecks("/health");

Log.Information("?? Manicure Studio API запускается...");
Log.Information("?? Swagger UI: https://localhost:PORT/swagger");
Log.Information("?? Health Check: https://localhost:PORT/health");

app.MapControllers();

app.Run();
