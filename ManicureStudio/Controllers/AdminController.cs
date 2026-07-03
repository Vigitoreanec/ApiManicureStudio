using ManicureStudio.Bot.DTOs;
using ManicureStudio.API.APIResult;
using ManicureStudio.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using Telegram.Bot;

namespace ManicureStudio.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AdminController(ITelegramBotClient botClient,
                                 AppDbContext context,
                                 IConfiguration configuration,
                                 ILogger<AdminController> logger) : ControllerBase
    {
        private readonly ITelegramBotClient _botClient = botClient;
        private readonly AppDbContext _context = context;
        private readonly IConfiguration _configuration = configuration;
        private readonly ILogger<AdminController> _logger = logger;

        [HttpGet("status")]
        [SwaggerOperation(Summary = "Статус бота")]
        [SwaggerResponse(200, "Статус получен", typeof(ApiResult<BotStatusResponse>))]
        [SwaggerResponse(500, "Ошибка сервера")]
        public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
        {
            try
            {
                var me = await _botClient.GetMe(cancellationToken);
                var webhookInfo = await _botClient.GetWebhookInfo(cancellationToken);
                var webhookUrl = _configuration["TelegramBot:WebhookUrl"];

                var status = new BotStatusResponse
                {
                    Status = "active",
                    Timestamp = DateTime.UtcNow,
                    BotName = me.FirstName,
                    BotUsername = me.Username!,
                    BotId = me.Id,
                    IsWebhookSet = webhookInfo.Url == webhookUrl,
                    WebhookUrl = webhookInfo.Url ?? "Не установлен",
                    PendingUpdates = webhookInfo.PendingUpdateCount
                };

                return Ok(ApiResult<BotStatusResponse>.Success(status, "Статус получен"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения статуса бота");
                return StatusCode(500, ApiResult<object>.Failure("Ошибка получения статуса", ex.Message));
            }
        }

        [HttpGet("statistics")]
        [SwaggerOperation(Summary = "Получить статистику бота")]
        [SwaggerResponse(200, "Статистика получена", typeof(ApiResult<BotStatusResponse>))]
        [SwaggerResponse(500, "Ошибка сервера")]
        public async Task<IActionResult> GetStatistics(CancellationToken cancellationToken)
        {
            try
            {
                var totalUsers = await _context.TelegramUsers.CountAsync(cancellationToken);
                var activeUsers = await _context.TelegramUsers
                    .CountAsync(u => u.UpdatedAt > DateTime.Now.AddDays(-7), cancellationToken);

                var today = DateTime.Today;
                var todayAppointments = await _context.Appointments
                    .CountAsync(a => a.StartTime.Date == today && !a.IsDeleted, cancellationToken);

                var totalAppointments = await _context.Appointments
                    .CountAsync(a => !a.IsDeleted, cancellationToken);

                var statistics = new BotStatisticsResponse
                {
                    TotalUsers = totalUsers,
                    ActiveUsersLast7Days = activeUsers,
                    TodayAppointments = todayAppointments,
                    TotalAppointments = totalAppointments,
                    Timestamp = DateTime.UtcNow
                };

                return Ok(ApiResult<BotStatisticsResponse>.Success(statistics, "Статистика получена"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения статистики");
                return StatusCode(500, ApiResult<object>.Failure("Ошибка получения статистики", ex.Message));
            }
        }

        [HttpGet("ping")]
        [SwaggerOperation(Summary = "Проверка работоспособности webhook")]
        [SwaggerResponse(200, "Webhook работает", typeof(ApiResult<object>))]
        public IActionResult Ping()
        {
            return Ok(ApiResult<object>.Success(
                new { Status = "ok", Timestamp = DateTime.UtcNow },
                "Pong"));
        }
    }
}
