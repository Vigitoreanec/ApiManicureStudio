using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace ManicureStudio.Bot
{
    public class BotHostedService : BackgroundService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BotHostedService> _logger;

        public BotHostedService(ITelegramBotClient botClient,
                                IServiceProvider serviceProvider,
                                ILogger<BotHostedService> logger)
        {
            _botClient = botClient;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Запуск Telegram бота...");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var updateHandler = scope.ServiceProvider.GetRequiredService<IUpdateHandler>();

                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = new[]
                    {
                    UpdateType.Message,
                    UpdateType.CallbackQuery,
                    UpdateType.EditedMessage
                    },
                    DropPendingUpdates = true
                };

                _botClient.StartReceiving(
                    updateHandler.HandleUpdateAsync,
                    updateHandler.HandleErrorAsync,
                    receiverOptions,
                    stoppingToken
                );

                _logger.LogInformation("✅ Бот успешно запущен и готов к работе!");

                try
                {
                    await Task.Delay(-1, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("⏹️ Бот останавливается...");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при запуске бота");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("⏹️ Остановка бота...");
            await base.StopAsync(cancellationToken);
        }

    }
}
