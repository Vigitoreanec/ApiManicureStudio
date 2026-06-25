using ManicureStudio.Bot.Repositories;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ManicureStudio.Bot
{
    public class UpdateHandler : IUpdateHandler
    {
        private readonly IBotService _botService;
        private readonly BotExceptionHandler _exceptionHandler;
        private readonly ILogger<UpdateHandler> _logger;

        public UpdateHandler(IBotService botService,
                             BotExceptionHandler exceptionHandler,
                             ILogger<UpdateHandler> logger)
        {
            _botService = botService;
            _exceptionHandler = exceptionHandler;
            _logger = logger;
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Ошибка в Telegram Bot API (Source: {Source})", source);

            if (exception is TaskCanceledException)
            {
                _logger.LogWarning("Операция была отменена");
            }

            return Task.CompletedTask;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                switch (update.Type)
                {
                    case UpdateType.Message:
                        await HandleMessage(update.Message, cancellationToken);
                        break;

                    case UpdateType.CallbackQuery:
                        await HandleCallback(update.CallbackQuery, cancellationToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                var chatId = GetChatId(update);
                if (chatId > 0)
                {
                    await _exceptionHandler.HandleExceptionAsync(chatId, ex, cancellationToken);
                }
                else
                {
                    _logger.LogError(ex, "Ошибка обработки обновления без ChatId");
                }
            }
        }

        private async Task HandleCallback(CallbackQuery? callbackQuery, CancellationToken cancellationToken)
        {
            if (callbackQuery == null) return;

            _logger.LogInformation(
                "Callback от {UserId}: {Data}",
                callbackQuery.From.Id,
                callbackQuery.Data);

            await _botService.HandleCallback(callbackQuery);
        }

        private async Task HandleMessage(Message? message, CancellationToken cancellationToken)
        {
            if (message == null) return;

            _logger.LogInformation(
                "Сообщение от {UserId} ({Username}): {Text}",
                message.From?.Id,
                message.From?.Username,
                message.Text ?? "[не текст]");

            if (message.Type == MessageType.Text)
            {
                if (message.Text == "/start")
                {
                    await _botService.HandleStart(message);
                    return;
                }

                await _botService.HandleTextMessage(message);
            }
            else
            {
                await _botService.HandleNonTextMessage(message);
            }
        }

        private long GetChatId(Update update)
        {
            return update.Message?.Chat?.Id ?? 
                update.CallbackQuery?.Message?.Chat?.Id ?? 
                0;
        }
    }
}
