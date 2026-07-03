using ManicureStudio.Bot.Infrastructure;
using ManicureStudio.Bot.Interfaces;
using ManicureStudio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static System.Net.Mime.MediaTypeNames;

namespace ManicureStudio.Bot.Handlers
{
    public class ServiceHandler(ITelegramBotClient bot,
                                AppDbContext context,
                                ISessionService sessionService,
                                ILogger<ServiceHandler> logger,
                                BotExceptionHandler exceptionHandler)
    {
        private readonly ITelegramBotClient _bot = bot;
        private readonly AppDbContext _context = context;
        private readonly ISessionService _sessionService = sessionService;
        private readonly ILogger<ServiceHandler> _logger = logger;
        private readonly BotExceptionHandler _exceptionHandler = exceptionHandler;

        // 2 блок
        public async Task ShowCategories(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;
            var messageId = callback.Message.MessageId;
            try
            {
                _logger.LogInformation("Показ категорий для пользователя {ChatId}", chatId);

                var user = await _sessionService.GetUser(chatId);
                if (user == null)
                {
                    _logger.LogWarning("Пользователь {ChatId} не найден, создаем...", chatId);
                    user = await _sessionService.GetOrCreateUser(
                        chatId,
                        callback.From.Username,
                        callback.From.FirstName,
                        callback.From.LastName);
                }

                await _sessionService.UpdateSessionData(chatId, data =>
                {
                    data.SelectedCategoryId = null;
                    data.SelectedServiceIds.Clear();          // Очищаем услуги
                });

                await _sessionService.UpdateStep(chatId, "select_service");
                
                var categories = await _context.ServiceCategories
                    .Where(c => !c.IsDeleted)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                if (categories.Count == 0)
                {

                    await _bot.SendMessage(
                        chatId,
                        "😔 К сожалению, услуги временно недоступны.",
                        replyMarkup: KeyboardService.GetMainMenuKeyboard());
                    return;
                }


                var text = "📋 *Выберите категорию услуг:*\n\n" +
                          "Нажмите на категорию, чтобы увидеть доступные услуги.";

                await _bot.EditMessageText(
                    chatId,
                    messageId,
                    text,
                    replyMarkup: KeyboardService.GetCategoriesKeyboard(categories),
                    parseMode: ParseMode.Markdown);

                _logger.LogInformation("Показано {Count} категорий для пользователя {ChatId}",
                    categories.Count, chatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в ShowCategories для {ChatId}", chatId);
                await _exceptionHandler.HandleExceptionAsync(chatId, ex);
            }
        }

        // 3 блок
        public async Task HandleCategorySelection(CallbackQuery callback, int categoryId)
        {
            var chatId = callback.Message.Chat.Id;
            var messageId = callback.Message.MessageId;

            var category = await _context.ServiceCategories
                .FirstOrDefaultAsync(c => c.Id == categoryId && !c.IsDeleted);

            if (category == null)
            {
                await _bot.AnswerCallbackQuery(
                    callback.Id,
                    "❌ Категория не найдена",
                    showAlert: true);
                return;
            }

            await _sessionService.UpdateSessionData(chatId, data =>
            {
                data.SelectedCategoryId = categoryId;
                data.SelectedServiceIds.Clear();
            });


            var services = await _context.Services
                .Where(s => s.CategoryId == categoryId   // Только эта категория
                    && !s.IsDeleted
                    && s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            if (services.Count == 0)
            {
                await _bot.AnswerCallbackQuery(
                    callback.Id,
                    $"😔 В категории \"{category.Name}\" пока нет услуг",
                    showAlert: true);
                
                await ShowCategories(callback);
                return;
            }

            var sessionData = await _sessionService.GetSessionData(chatId);
            var text = $"💅 *{category.Name}*\n\n" +
                          $"Выберите услуги (можно несколько):\n" +
                          $"*(Выбрано: {sessionData.SelectedServiceIds.Count} услуг)*";

            await _sessionService.UpdateStep(chatId, "select_services");

            await _bot.EditMessageText(
                    chatId,
                    messageId,
                    text,
                    replyMarkup: KeyboardService.GetServicesKeyboard(services, sessionData.SelectedServiceIds),
                    parseMode: ParseMode.Markdown);

            _logger.LogInformation("Показано {Count} услуг в категории {CategoryId}",
                services.Count, categoryId);
        }

        // 5-6 юлок
        public async Task HandleServiceSelection(CallbackQuery callback, int serviceId)
        {
            var chatId = callback.Message.Chat.Id;
            var sessionData = await _sessionService.GetSessionData(chatId);

            if (!sessionData.SelectedCategoryId.HasValue)
            {
                await _bot.AnswerCallbackQuery(callback.Id, "❌ Сначала выберите категорию", true);
                return;
            }

            await _sessionService.UpdateSessionData(chatId, data =>
            {
                if (data.SelectedServiceIds.Contains(serviceId))
                    data.SelectedServiceIds.Remove(serviceId);
                else
                    data.SelectedServiceIds.Add(serviceId);
            });

            sessionData = await _sessionService.GetSessionData(chatId);

            var services = await _context.Services
                .Where(s => s.CategoryId == sessionData.SelectedCategoryId && !s.IsDeleted)
                .OrderBy(s => s.Name)
                .ToListAsync();
            //
            var category = await _context.ServiceCategories
                    .FirstOrDefaultAsync(c => c.Id == sessionData.SelectedCategoryId.Value);

            var categoryName = category?.Name ?? "Услуги";

            var text = $"💅 *{categoryName}*\n\n" +
                          $"Выберите услуги (можно несколько):\n" +
                          $"*(Выбрано: {sessionData.SelectedServiceIds.Count} услуг)*";

            await _bot.EditMessageText(
                    chatId,
                    callback.Message.MessageId,
                    text,
                    replyMarkup: KeyboardService.GetServicesKeyboard(services, sessionData.SelectedServiceIds),
                    parseMode: ParseMode.Markdown);

            await _bot.AnswerCallbackQuery(
                    callback.Id,
                    sessionData.SelectedServiceIds.Contains(serviceId)
                        ? "✅ Услуга добавлена"
                        : "❌ Услуга удалена");

        }

        //3 блок к мастерам
        public async Task HandleServiceConfirm(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;

            var sessionData = await _sessionService.GetSessionData(chatId);
            if (sessionData.SelectedServiceIds.Count == 0)
            {
                await _bot.AnswerCallbackQuery(
                    callback.Id,
                    "❌ Выберите хотя бы одну услугу",
                    showAlert: true);
                return;
            }

            var services = await _context.Services
                    .Where(s => sessionData.SelectedServiceIds.Contains(s.Id) && !s.IsDeleted)
                    .ToListAsync();

            var totalPrice = services.Sum(s => s.Price);
            var totalDuration = services.Sum(s => s.DurationMinutes);

            await _bot.AnswerCallbackQuery(
                    callback.Id,
                    $"✅ Выбрано {sessionData.SelectedServiceIds.Count} услуг на {totalPrice:F2}₽",
                    showAlert: true);

            await ShowServicesForBooking(callback);

        }

        // блок 8
        public async Task ShowServicesForBooking(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;
            var messageId = callback.Message.MessageId;

            var sessionData = await _sessionService.GetSessionData(chatId);

            var services = await _context.Services
                    .Where(s => sessionData.SelectedServiceIds.Contains(s.Id) && !s.IsDeleted)
                    .ToListAsync();

            if (services.Count == 0)
            {
                await _bot.SendMessage(
                    chatId,
                    "😔 Выбранные услуги не найдены.",
                    replyMarkup: KeyboardService.GetMainMenuKeyboard());
                return;
            }

            var totalDuration = services.Sum(s => s.DurationMinutes);
            var totalPrice = services.Sum(s => s.Price);

            var text = new StringBuilder($"✅ *Выбраны услуги:*\n\n");
            foreach (var service in services)
            {
                text.Append("• ").Append(service.Name).Append(" - ").Append(service.DurationMinutes)
                    .Append(" мин, ").Append(service.Price.ToString("F2")).Append("₽\n");
            }
            text.Append("\n⏱ *Общая длительность:* ").Append(totalDuration).Append(" мин")
                .Append("\n💰 *Итоговая сумма:* ").Append(totalPrice.ToString("F2")).Append("₽")
                .Append("\n\n👨‍🔧 *Выберите мастера:*");

            var fullText = text.ToString();

            var masters = await _context.Masters
                    .Where(m => !m.IsDeleted && m.IsActive)
                    .OrderBy(m => m.FirstName)
                    .ToListAsync();

            if (masters.Count == 0)
            {
                await _bot.EditMessageText(
                    chatId,
                    messageId,
                    "😔 К сожалению, мастера временно недоступны.\n\n" +
                    "Попробуйте позже или свяжитесь с администрацией.",
                    replyMarkup: KeyboardService.GetMainMenuKeyboard());
                return;
            }

            await _sessionService.UpdateStep(chatId, "select_master");

            await _bot.EditMessageText(
                chatId,
                messageId,
                fullText,
                replyMarkup: KeyboardService.GetMastersKeyboard(masters),
                parseMode: ParseMode.Markdown);
        }

        public async Task ClearServices(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;

            await _sessionService.UpdateSessionData(chatId, data =>
            {
                data.SelectedServiceIds.Clear();
            });

            var sessionData = await _sessionService.GetSessionData(chatId);

            var services = await _context.Services
                .Where(s => s.CategoryId == sessionData.SelectedCategoryId && !s.IsDeleted)
                .OrderBy(s => s.Name)
                .ToListAsync();

            var category = await _context.ServiceCategories
                    .FirstOrDefaultAsync(c => c.Id == sessionData.SelectedCategoryId);

            var categoryName = category?.Name ?? "Услуги";

            var text = $"💅 *{categoryName}*\n\n" +
                      $"Выберите услуги (можно несколько):\n" +
                      $"*(Выбрано: 0 услуг)*";

            await _bot.EditMessageText(
                    chatId,
                    callback.Message.MessageId,
                    text,
                    replyMarkup: KeyboardService.GetServicesKeyboard(services, sessionData.SelectedServiceIds),
                    parseMode: ParseMode.Markdown);

            await _bot.AnswerCallbackQuery(callback.Id, "🗑️ Все услуги очищены");
        }

    }
}
