using ManicureStudio.API;
using ManicureStudio.API.Middleware;
using ManicureStudio.Bot.Exceptions;
using ManicureStudio.Infrastructure;
using ManicureStudio.Infrastructure.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>();
builder.Configuration.AddEnvironmentVariables();

//------ API
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler =
                System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.PropertyNamingPolicy =
        System.Text.Json.JsonNamingPolicy.CamelCase;
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

builder.Services.AddBotServices(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();

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
        c.RoutePrefix = "swagger";
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
Log.Information("✅ API успешно запущен");
Log.Information("📚 Swagger UI: {Url}",
    app.Urls.FirstOrDefault() ?? "https://localhost:7023/swagger");

app.MapControllers();

app.Run();
