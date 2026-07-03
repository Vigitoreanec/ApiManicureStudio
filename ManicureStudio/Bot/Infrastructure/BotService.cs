using ManicureStudio.Bot.Handlers;
using ManicureStudio.Bot.Interfaces;
using ManicureStudio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ManicureStudio.Bot.Infrastructure
{
    public class BotService(ITelegramBotClient bot,
                            ISessionService sessionService,
                            ILogger<BotService> logger,
                            BotExceptionHandler exceptionHandler,
                            ServiceHandler serviceHandler,
                            AppointmentHandler appointmentHandler,
                            ProfileHandler profileHandler,
                            ClientHandler clientHandler,
                            AppDbContext context) : IBotService
    {
        private readonly ITelegramBotClient _bot = bot;
        private readonly ISessionService _sessionService = sessionService;
        private readonly ILogger<BotService> _logger = logger;
        private readonly BotExceptionHandler _exceptionHandler = exceptionHandler;

        private readonly AppDbContext _context = context;

        private readonly ServiceHandler _serviceHandler = serviceHandler;
        private readonly AppointmentHandler _appointmentHandler = appointmentHandler;
        private readonly ProfileHandler _profileHandler = profileHandler;
        private readonly ClientHandler _clientHandler = clientHandler;

        public async Task HandleCallback(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;
            var data = callback.Data;

            _logger.LogInformation("🔍 ПОЛУЧЕН CALLBACK: {Data}", data);

            try
            {
                await _bot.AnswerCallbackQuery(callback.Id);

                var user = await _sessionService.GetUser(chatId);
                if (user == null)
                {
                    _logger.LogWarning("Пользователь {UserId} не найден. Создаем...", callback.From.Id);
                    user = await _sessionService.GetOrCreateUser(
                        callback.From.Id,
                        callback.From.Username,
                        callback.From.FirstName,
                        callback.From.LastName);
                }

                switch (data)
                {
                    case "book_without_comment":
                        _logger.LogInformation("✅ Вызван BookWithoutComment для {ChatId}", chatId);
                        await _clientHandler.BookWithoutComment(callback);
                        return;

                    case "add_comment":
                        _logger.LogInformation("✅ Вызван AddComment для {ChatId}", chatId);
                        await _clientHandler.AddComment(callback);
                        return;

                    case "skip_comment":
                        _logger.LogInformation("✅ Вызван SkipComment для {ChatId}", chatId);
                        await _clientHandler.SkipComment(callback);
                        return;

                    case "back_to_phone":
                        _logger.LogInformation("✅ Вызван BackToPhone для {ChatId}", chatId);
                        await _clientHandler.BackToPhone(callback);
                        return;

                    case "back_to_phone_actions":
                        _logger.LogInformation("✅ Вызван BackToPhoneActions для {ChatId}", chatId);
                        await _clientHandler.BackToPhoneActions(callback);
                        return;

                    case "book_confirm":
                        _logger.LogInformation("✅ ConfirmBooking для {ChatId}", chatId);
                        await _appointmentHandler.ConfirmBooking(callback);
                        return;

                    case "main_menu":
                        await ShowMainMenu(callback);
                        break;

                    case "menu_bookings":
                        await _profileHandler.ShowBookings(callback);
                        return;

                    case "back":
                        await HandleBack(callback);
                        return;

                    case "cancel":
                        await HandleCancel(callback);
                        return;

                    case "profile":
                        await _profileHandler.ShowProfile(callback);
                        return;
                }


                var parts = data.Split('_');
                var action = parts[0];

                switch (action)
                {
                    case "menu":
                        await HandleMenuCallback(callback, parts);
                        break;

                    case "cat": // Категория
                        await _serviceHandler.HandleCategorySelection(callback, int.Parse(parts[1]));
                        break;
                    case "serv": // Услуга
                        await _serviceHandler.HandleServiceSelection(callback, int.Parse(parts[1]));
                        break;
                    case "confirm": // выбор услуг
                        await _serviceHandler.HandleServiceConfirm(callback);
                        break;
                    case "clear": // Очистить
                        await _serviceHandler.ClearServices(callback);
                        break;

                    case "master":
                        await _appointmentHandler.HandleMasterSelection(callback, int.Parse(parts[1]));
                        break;

                    case "date":
                        await _appointmentHandler.HandleDateSelection(callback, DateTime.Parse(parts[1]));
                        break;

                    case "time":
                            var time = parts[2]; // Результат: "10:00"

                            await _appointmentHandler.HandleTimeSelection(callback, TimeSpan.Parse(time));
                            break;
                        
                    case "page":
                        await _appointmentHandler.HandleTimePage(callback, int.Parse(parts[1]));
                        break;

                    case "cancel_appt":
                        await _appointmentHandler.CancelAppointment(callback, int.Parse(parts[1]));
                        break;
                    
                    case "confirm_cancel":
                        await _appointmentHandler.ConfirmCancel(callback, int.Parse(parts[1]));
                        break;

                    case "pay_card":
                    case "pay_cash":
                    case "pay_online":
                        await _appointmentHandler.HandlePaymentSelection(callback, parts[0].Replace("pay_", ""));
                        break;
                    default:
                        await _bot.SendMessage(
                            chatId,
                            " Пожалуйста, выберите действие из меню.",
                            replyMarkup: KeyboardService.GetMainMenuKeyboard());
                        break;
                }
            }
            catch (Exception ex)
            {
                await _exceptionHandler.HandleExceptionAsync(chatId, ex);
            }
        }
        public async Task HandleStart(Message message)
        {
            var user = await _sessionService.GetOrCreateUser(message.From.Id,
                                                             message.From.Username,
                                                             message.From.FirstName,
                                                             message.From.LastName);

            await _sessionService.UpdateStep(user.TelegramId, "main_menu");

            var text = $"👋 Здравствуйте, {user.FirstName ?? "гость"}!\n\n" +
                      "Добро пожаловать в Bot для записи на Маникюр!\n" +
                      "Я помогу вам записаться на процедуры.\n\n" +
                      "Выберите действие:";
            var textInfo = "Пользователь { TelegramId} - " + user.FirstName + "(" + DateTime.Now + ") " + "активировал бота";

            await _bot.SendMessage(
                user.TelegramId,
                text,
                replyMarkup: KeyboardService.GetMainMenuKeyboard());

            _logger.LogInformation(textInfo, user.TelegramId);
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

            // Обработка сообщений
            switch (step)
            {

                case "awaiting_client_name":
                    await _clientHandler.HandleClientName(message);
                    break;

                case "awaiting_client_phone":
                    await _clientHandler.HandleClientPhone(message);
                    break;

                case "awaiting_client_comment":
                    await _clientHandler.HandleClientComment(message);
                    break;

                default:
                    await _bot.SendMessage(
                        chatId,
                        "⚠️ Я не понял команду. Пожалуйста, используйте кнопки меню.",
                        replyMarkup: KeyboardService.GetMainMenuKeyboard());
                    break;
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


        /*private async Task ShowBookings(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;
            await _bot.EditMessageText(
                chatId,
                callback.Message.MessageId,
                "📅 *Мои записи*\n\nУ вас пока нет активных записей.",
                replyMarkup: KeyboardService.GetBackToMenuKeyboard(),
                parseMode: ParseMode.Markdown);
        }*/

        /*private async Task ShowServices(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;
            await _bot.EditMessageText(
                chatId,
                callback.Message.MessageId,
                "📋 *Услуги*\n\nРаздел временно недоступен. Скоро появится!",
                replyMarkup: KeyboardService.GetBackToMenuKeyboard(),
                parseMode: ParseMode.Markdown);
        }*/

        private async Task HandleMenuCallback(CallbackQuery callback, string[] parts)
        {
            var menuAction = parts.Length > 1 ? parts[1] : null;

            switch (menuAction)
            {
                case "services":
                    await _serviceHandler.ShowCategories(callback);
                    break;

                case "bookings":
                    await _profileHandler.ShowBookings(callback);
                    break;

                case "profile":
                    await _profileHandler.ShowProfile(callback);
                    break;

                default:
                    await ShowMainMenu(callback);
                    break;
            }
        }
        private async Task ShowMainMenu(CallbackQuery callback)
        {
            await _bot.EditMessageText(callback.Message.Chat.Id,
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
                case "select_service":
                    await _serviceHandler.ShowCategories(callback);
                    break;

                case "select_services":
                    await _serviceHandler.ShowServicesForBooking(callback);
                    break;

                case "select_master":
                    await _appointmentHandler.ShowMasters(callback);
                    break;

                case "select_date":
                    await _appointmentHandler.ShowDates(callback);
                    break;

                case "select_time":
                    await _appointmentHandler.ShowTimeSlots(callback);
                    break;

                case "awaiting_client_name":
                case "awaiting_client_phone":
                case "awaiting_client_comment":
                    await _appointmentHandler.ShowTimeSlots(callback);
                    break;

                default:
                    await ShowMainMenu(callback);
                    break;
            }
        }

    }
}
