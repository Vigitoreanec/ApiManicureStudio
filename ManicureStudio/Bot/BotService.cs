using ManicureStudio.Bot.Interfaces;
using ManicureStudio.Bot.Repositories;
using System.IO;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ManicureStudio.Bot
{
    public class BotService(ITelegramBotClient bot,
                            ISessionService sessionService,
                            ILogger<BotService> logger,
                            BotExceptionHandler exceptionHandler) : IBotService
    {
        private readonly ITelegramBotClient _bot = bot;
        private readonly ISessionService _sessionService = sessionService;
        private readonly ILogger<BotService> _logger = logger;
        private readonly BotExceptionHandler _exceptionHandler = exceptionHandler;

        public async Task HandleCallback(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;
            var data = callback.Data;

            _logger.LogInformation("Callback от {UserId}: {Data}", callback.From.Id, data);

            try
            {
                await _bot.AnswerCallbackQuery(callback.Id);

                var parts = data.Split('_');
                var action = parts[0];

                switch (action)
                {
                    // Главное меню
                    case "menu":
                        await HandleMenuCallback(callback, parts);
                        break;

                    // Навигация
                    case "back":
                        await HandleBack(callback);
                        break;

                    case "cancel":
                        await HandleCancel(callback);
                        break;

                    case "main_menu":
                        await ShowMainMenu(callback);
                        break;

                    default:
                        await _bot.SendMessage(
                            chatId,
                            "⚠️ Неизвестная команда. Пожалуйста, выберите действие из меню.",
                            replyMarkup: KeyboardService.GetMainMenuKeyboard());
                        break;
                }
            }
            catch (Exception ex)
            {
                await _exceptionHandler.HandleExceptionAsync(chatId, ex);
            }
        }


        public async Task HandleNonTextMessage(Message message)
        {
            var chatId = message.Chat.Id;
            await _bot.SendMessage(
                chatId,
                "ℹ️ Пожалуйста, отправьте текстовое сообщение или используйте кнопки.",
                replyMarkup: KeyboardService.GetMainMenuKeyboard());
        }

        public async Task HandleStart(Message message)
        {
            var user = await _sessionService.GetOrCreateUser(
             message.From.Id,
             message.From.Username,
             message.From.FirstName,
             message.From.LastName);

            await _sessionService.UpdateStep(user.TelegramId, "main_menu");

            var text = $"👋 Здравствуйте, {user.FirstName ?? "гость"}!\n\n" +
                      "Добро пожаловать в Bot для записи на Маникюр!\n" +
                      "Я помогу вам записаться на процедуры.\n\n" +
                      "Выберите действие:";

            await _bot.SendMessage(
                user.TelegramId,
                text,
                replyMarkup: KeyboardService.GetMainMenuKeyboard());

            _logger.LogInformation("Пользователь {TelegramId} активировал бота", user.TelegramId);
        }

        public async Task HandleTextMessage(Message message)
        {
            var chatId = message.Chat.Id;
            var user = await _sessionService.GetUser(chatId);

            if (user == null)
            {
                await HandleStart(message);
                return;
            }

            var step = user.CurrentStep;

            // Обработка текстовых сообщений в зависимости от шага
            switch (step)
            {
                // Здесь будут добавляться обработчики для разных шагов
                // Например:
                // case "awaiting_name":
                //     await HandleClientName(message);
                //     break;

                default:
                    await _bot.SendMessage(
                        chatId,
                        "⚠️ Я не понял команду. Пожалуйста, используйте кнопки меню.",
                        replyMarkup: KeyboardService.GetMainMenuKeyboard());
                    break;
            }
        }

        public async Task SendErrorMessage(long chatId, string message)
        {
            try
            {
                await _bot.SendMessage(
                    chatId,
                    $"❌ {message}",
                    replyMarkup: KeyboardService.GetMainMenuKeyboard());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки сообщения об ошибке");
            }
        }
        private async Task HandleMenuCallback(CallbackQuery callback, string[] parts)
        {
            var menuAction = parts.Length > 1 ? parts[1] : null;

            switch (menuAction)
            {
                case "services":
                    await ShowServices(callback);
                    break;
                case "bookings":
                    await ShowBookings(callback);
                    break;
                case "profile":
                    await ShowProfile(callback);
                    break;
                default:
                    await ShowMainMenu(callback);
                    break;
            }
        }

        private async Task ShowProfile(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;
            var user = await _sessionService.GetUser(chatId);

            if (user == null)
            {
                await ShowMainMenu(callback);
                return;
            }

            var text = $"👤 *Ваш профиль*\n\n" +
                      $"🆔 ID: {user.TelegramId}\n" +
                      $"👤 Имя: {user.FirstName ?? "Не указано"}\n" +
                      $"📝 Фамилия: {user.LastName ?? "Не указана"}\n" +
                      $"🔑 Username: @{user.Username ?? "Не указан"}\n" +
                      $"📅 Зарегистрирован: {user.CreatedAt:dd.MM.yyyy HH:mm}";

            await _bot.EditMessageText(
                chatId,
                callback.Message.MessageId,
                text,
                replyMarkup: KeyboardService.GetBackToMenuKeyboard(),
                parseMode: ParseMode.Markdown);
        }

        private async Task ShowBookings(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;
            await _bot.EditMessageText(
                chatId,
                callback.Message.MessageId,
                "📅 *Мои записи*\n\nУ вас пока нет активных записей.",
                replyMarkup: KeyboardService.GetBackToMenuKeyboard(),
                parseMode: ParseMode.Markdown);
        }

        private async Task ShowServices(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;
            await _bot.EditMessageText(
                chatId,
                callback.Message.MessageId,
                "📋 *Услуги*\n\nРаздел временно недоступен. Скоро появится!",
                replyMarkup: KeyboardService.GetBackToMenuKeyboard(),
                parseMode: ParseMode.Markdown);
        }

        private async Task ShowMainMenu(CallbackQuery callback)
        {
            await _bot.EditMessageText(
            callback.Message.Chat.Id,
            callback.Message.MessageId,
            "🏠 *Главное меню*\n\nВыберите действие:",
            replyMarkup: KeyboardService.GetMainMenuKeyboard(),
            parseMode: ParseMode.Markdown);
        }

        private async Task HandleCancel(CallbackQuery callback)
        {
            await _sessionService.ClearSession(callback.From.Id);
            await _bot.EditMessageText(
                callback.Message.Chat.Id,
                callback.Message.MessageId,
                "❌ Действие отменено.\n\nВыберите действие:",
                replyMarkup: KeyboardService.GetMainMenuKeyboard());
        }

        private async Task HandleBack(CallbackQuery callback)
        {
            var user = await _sessionService.GetUser(callback.From.Id);
            var step = user?.CurrentStep ?? "main_menu";

            switch (step)
            {
                default:
                    await ShowMainMenu(callback);
                    break;
            }
        }

    }
}
