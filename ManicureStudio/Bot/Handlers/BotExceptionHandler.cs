using ManicureStudio.Bot.Exceptions;
using ManicureStudio.Bot.Infrastructure;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace ManicureStudio.Bot.Handlers
{
    public class BotExceptionHandler(ITelegramBotClient bot,
                                     ILogger<BotExceptionHandler> logger,
                                     IConfiguration configuration)
    {
        private readonly ITelegramBotClient _bot = bot;
        private readonly ILogger<BotExceptionHandler> _logger = logger;
        private readonly long[] _adminChatIds = configuration.GetSection("TelegramBot:AdminChatIds")
                                                .Get<long[]>() ?? [];

        public async Task HandleExceptionAsync(long chatId,
                                               Exception exception,
                                               CancellationToken cancellationToken = default)
        {
            // Логируем ошибку
            _logger.LogError(exception, "Ошибка для пользователя {ChatId}", chatId);

            // Определяем тип ошибки и формируем сообщение
            var (userMessage, shouldNotifyAdmin) = GetUserMessage(exception);

            // Отправляем сообщение пользователю
            try
            {
                await _bot.SendMessage(
                    chatId: chatId,
                    text: userMessage,
                    parseMode: ParseMode.Markdown,
                    replyMarkup: KeyboardService.GetMainMenuKeyboard(),
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception sendEx)
            {
                _logger.LogError(sendEx, "Ошибка при отправке сообщения об ошибке пользователю {ChatId}", chatId);
            }

            if (shouldNotifyAdmin)
            {
                await InformAdminAsync(exception, chatId, cancellationToken);
            }

        }

        private async Task InformAdminAsync(Exception exception, long chatId, CancellationToken cancellationToken)
        {
            if (_adminChatIds.Length == 0)
            {
                _logger.LogWarning("Не настроены ID администраторов для уведомлений");
                return;
            }

            var message = $"🚨 *Критическая ошибка в боте*\n\n" +
                         $"Пользователь: `{chatId}`\n" +
                         $"Ошибка: `{exception.GetType().Name}`\n" +
                         $"Сообщение: `{exception.Message}`\n" +
                         $"Время: `{DateTime.Now:dd.MM.yyyy HH:mm:ss}`";

            foreach (var adminId in _adminChatIds)
            {
                try
                {
                    await _bot.SendMessage(
                        adminId,
                        message,
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Не удалось отправить уведомление администратору {AdminId}", adminId);
                }
            }
        }

        private (string userMessage, bool shouldInfoAdmin) GetUserMessage(Exception exception)
        {
            return exception switch
            {
                BotException.ValidationException validationEx => (validationEx.UserMessage, false),
                BotException.NotFoundException notFoundEx => (notFoundEx.UserMessage, false),
                BotException.ConflictException conflictEx => (conflictEx.UserMessage, false),
                BotException botEx => (botEx.UserMessage, botEx.InfoAdmin),
                _ => ("❌ Произошла техническая ошибка. Мы уже работаем над её исправлением.", true)
            };
        }
    }
}
