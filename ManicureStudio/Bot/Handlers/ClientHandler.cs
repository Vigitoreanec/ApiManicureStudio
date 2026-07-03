using ManicureStudio.Bot.Infrastructure;
using ManicureStudio.Bot.Interfaces;
using ManicureStudio.Core.Entities;
using ManicureStudio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ManicureStudio.Bot.Handlers
{
    public class ClientHandler(ITelegramBotClient bot,
                               AppDbContext context,
                               ISessionService sessionService,
                               ILogger<ClientHandler> logger)
    {
        private readonly ITelegramBotClient _bot = bot;
        private readonly AppDbContext _context = context;
        private readonly ISessionService _sessionService = sessionService;
        private readonly ILogger<ClientHandler> _logger = logger;

        public async Task RequestClientName(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;

            await _sessionService.UpdateStep(chatId, "awaiting_client_name");

            await _bot.EditMessageText(
                chatId,
                callback.Message.MessageId,
                "📝 *Введите ваше имя:*\n\n" +
                "Например: *Анна Иванова*",
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "back") }
                }),
                parseMode: ParseMode.Markdown);
        }
        // 15 блок
        public async Task HandleClientName(Message message)
        {
            var chatId = message.Chat.Id;
            var name = message.Text?.Trim();

            if (string.IsNullOrEmpty(name) || name.Length < 3 || !Regex.IsMatch(name, @"^[а-яА-ЯёЁ\s-]+$"))
            {
                await _bot.SendMessage(
                    chatId,
                    "⚠️ Имя должно содержать минимум 3 символа, и только русские Буквы. Попробуйте еще раз:",
                    replyMarkup: KeyboardService.GetCancelKeyboard());
                return;
            }

            // Сохраняем имя в сессию
            await _sessionService.UpdateSessionData(chatId, data =>
            {
                data.TempName = name;
            });

            await _sessionService.UpdateStep(chatId, "awaiting_client_phone");

            await _bot.SendMessage(
                chatId,
                $"✅ Имя *{name}* сохранено!\n\n" +
                "📞 *Введите ваш номер телефона:*\n\n" +
                "Например: *+7 123 456 78 90*",
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "back") }
                }));
        }
        // 16-17 блок
        public async Task HandleClientPhone(Message message)
        {
            var chatId = message.Chat.Id;
            var phone = message.Text?.Trim();

            if (string.IsNullOrEmpty(phone))
            {
                await SendInvalidPhoneMessage(chatId);
                return;
            }

            string cleanedPhone = Regex.Replace(phone, @"[^\d+]", "");

            if (cleanedPhone.StartsWith("+7"))
            {
                cleanedPhone = string.Concat("8", cleanedPhone.AsSpan(2));
            }
            else if (cleanedPhone.StartsWith("7") && cleanedPhone.Length == 11)
            {
                cleanedPhone = string.Concat("8", cleanedPhone.AsSpan(1));
            }
            else if (cleanedPhone.StartsWith("9") && cleanedPhone.Length == 10)
            {
                cleanedPhone = "8" + cleanedPhone;
            }

            if (!Regex.IsMatch(cleanedPhone, @"^8\d{10}$"))
            {
                await SendInvalidPhoneMessage(chatId);
                return;
            }

            // Сохраняем телефон
            await _sessionService.UpdateSessionData(chatId, data =>
            {
                data.TempPhone = cleanedPhone;
            });

            // Проверяем, есть ли клиент в БД (Блок 17)
            var existingClient = await _context.Clients
                .FirstOrDefaultAsync(c => c.PhoneNumber == cleanedPhone && !c.IsDeleted);

            if (existingClient != null)
            {
                // Клиент найден - связываем с Telegram
                await _sessionService.LinkToClient(chatId, existingClient.Id);

                await _bot.SendMessage(
                    chatId,
                    $"✅ Вы уже зарегистрированы как *{existingClient.FirstName} {existingClient.LastName}*!\n\n" +
                    "📋 Переходим к подтверждению записи...",
                    parseMode: ParseMode.Markdown);
            }
            
            await ShowPhoneActions(chatId);
        }
        
        public async Task HandleClientComment(Message message)
        {
            var chatId = message.Chat.Id;
            var comment = message.Text?.Trim();

            await _sessionService.UpdateSessionData(chatId, data =>
            {
                data.ClientComment = comment;
            });

            await ShowFinalConfirmation(message);
        }
        public async Task BookWithoutComment(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;

            _logger.LogInformation("📞 BookWithoutComment ВЫЗВАН для {ChatId}", chatId);

            await _sessionService.UpdateSessionData(chatId, data =>
            {
                data.ClientComment = null;
            });

            _logger.LogInformation("✅ Комментарий очищен для {ChatId}", chatId);

            await _bot.AnswerCallbackQuery(callback.Id, "✅ Переход к подтверждению");

            _logger.LogInformation("📞 Вызов ShowFinalConfirmationFromCallback для {ChatId}", chatId);

            await ShowFinalConfirmationFromCallback(callback);
        }
        public async Task AddComment(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;

            await _sessionService.UpdateStep(chatId, "awaiting_client_comment");

            var sessionData = await _sessionService.GetSessionData(chatId);

            var text = $"✏️ *Введите ваш комментарий:*\n\n" +
                       $"👤 Имя: *{sessionData.TempName}*\n" +
                       $"📞 Телефон: *{sessionData.TempPhone}*\n\n" +
                       $"Напишите комментарий или нажмите кнопку для пропуска:";

            _logger.LogInformation("📞 AddComment ВЫЗВАН для {ChatId}", callback.Message.Chat.Id);

            await _bot.EditMessageText(
                chatId,
                callback.Message.MessageId,
                text,
                parseMode: ParseMode.Markdown,
                replyMarkup: KeyboardService.GetCommentKeyboard());
        }
        public async Task SkipComment(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;

            await _sessionService.UpdateSessionData(chatId, data =>
            {
                data.ClientComment = null;
            });

            await _bot.EditMessageText(
                chatId,
                callback.Message.MessageId,
                "⏭️ Комментарий пропущен.\n\n" +
                "Переходим к подтверждению записи...",
                replyMarkup: null);

            await ShowFinalConfirmationFromCallback(callback);
        }
        public async Task BackToPhoneActions(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;

            await _bot.AnswerCallbackQuery(callback.Id, "🔙 Возврат");

            await ShowPhoneActions(chatId);
        }
        public async Task BackToPhone(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;

            await _sessionService.UpdateStep(chatId, "awaiting_client_phone");

            await _bot.EditMessageText(
                chatId,
                callback.Message.MessageId,
                "📞 *Введите ваш номер телефона:*\n\n" +
                "Например: *+7 123 456 78 90*",
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "back") }
                }));
        }
        public async Task ShowFinalConfirmation(Message message)
        {
            var chatId = message.Chat.Id;
            var sessionData = await _sessionService.GetSessionData(chatId);

            var services = await _context.Services
                .Where(s => sessionData.SelectedServiceIds.Contains(s.Id) && !s.IsDeleted)
                .ToListAsync();

            var master = await _context.Masters
                .FirstOrDefaultAsync(m => m.Id == sessionData.SelectedMasterId && !m.IsDeleted);

            var totalPrice = services.Sum(s => s.Price);
            var totalDuration = services.Sum(s => s.DurationMinutes);

            var sb = new StringBuilder();
            sb.Append("📋 *Подтверждение записи*\n\n")
              .Append("👤 Клиент: *").Append(sessionData.TempName).Append("*\n")
              .Append("📞 Телефон: *").Append(sessionData.TempPhone).Append("*\n");

            
            sb.Append("👨‍🔧 Мастер: *").Append(master.FirstName).Append(' ').
                Append(master.LastName).Append("*\n");
            
            
            var dt = sessionData.SelectedDateTime.Value;
            sb.Append("📅 Дата: *")
                .Append(dt.ToString("dd.MM.yyyy"))
                .Append("*\n")
                .Append("🕐 Время: *")
                .Append(dt.ToString("HH:mm"))
                .Append("*\n\n");

            sb.Append("💅 *Услуги:*\n");
            foreach (var service in services)
            {
                sb.Append("  • ").Append(service.Name).Append(" - ")
                  .Append(service.DurationMinutes).Append(" мин, *");
                sb.AppendFormat("{0:F2}", service.Price);
                sb.Append("* ₽\n");
            }

            sb.Append("\n⏱ *Общая длительность:* ").Append(totalDuration).Append(" мин")
              .Append("\n💰 *Сумма:* *");
            sb.AppendFormat("{0:F2}", totalPrice);
            sb.Append("* ₽");

            if (!string.IsNullOrEmpty(sessionData.ClientComment))
            {
                sb.Append("\n\n📝 *Комментарий:* ").Append(sessionData.ClientComment);
            }

            sb.Append("\n\n✅ *Нажмите кнопку, чтобы подтвердить запись!*");

            var text = sb.ToString();

            await _bot.SendMessage(
                chatId,
                text,
                parseMode: ParseMode.Markdown,
                replyMarkup: KeyboardService.GetBookingKeyboard(totalPrice));
        }
        public async Task<Client> RegisterClient(long telegramId)
        {
            var sessionData = await _sessionService.GetSessionData(telegramId);

            var existingClient = await _context.Clients
                .FirstOrDefaultAsync(c => c.PhoneNumber == sessionData.TempPhone && !c.IsDeleted);

            if (existingClient != null)
            {
                await _sessionService.LinkToClient(telegramId, existingClient.Id);
                return existingClient;
            }

            // Создаем нового клиента
            var names = sessionData.TempName?.Split(' ', 2) ?? ["", ""];
            var client = new Client
            {
                FirstName = names[0] ?? "Unknown",
                LastName = names.Length > 1 ? names[1] : "",
                PhoneNumber = sessionData.TempPhone,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _context.Clients.AddAsync(client);
            await _context.SaveChangesAsync();

            // Связываем с Telegram
            await _sessionService.LinkToClient(telegramId, client.Id);

            _logger.LogInformation("Зарегистрирован новый клиент: {FirstName} {LastName}, Телефон: {Phone}",
                client.FirstName, client.LastName, client.PhoneNumber);

            return client;
        }

        public async Task ShowFinalConfirmationFromCallback(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;
            _logger.LogInformation("📞 ShowFinalConfirmationFromCallback ВЫЗВАН для {ChatId}", chatId);
            var sessionData = await _sessionService.GetSessionData(chatId);
            
            if (sessionData.SelectedDateTime == null || sessionData.SelectedMasterId == null || !sessionData.SelectedServiceIds.Any())
            {
                _logger.LogWarning("⚠️ Недостаточно данных для подтверждения");
                await _bot.SendMessage(chatId, "❌ Ошибка: данные не найдены. Начните заново.");
                return;
            }




            _logger.LogInformation("📊 Данные сессии: Services={Count}, Master={MasterId}, DateTime={DateTime}",
                                   sessionData.SelectedServiceIds.Count,
                                   sessionData.SelectedMasterId,
                                   sessionData.SelectedDateTime);

            var services = await _context.Services
                .Where(s => sessionData.SelectedServiceIds.Contains(s.Id) && !s.IsDeleted)
                .ToListAsync();

            var master = await _context.Masters
                .FirstOrDefaultAsync(m => m.Id == sessionData.SelectedMasterId && !m.IsDeleted);

            var totalPrice = services.Sum(s => s.Price);
            var totalDuration = services.Sum(s => s.DurationMinutes);

            var sb = new StringBuilder();
            sb.Append("📋 *Подтверждение записи*\n\n")
              .Append("👤 Клиент: *").Append(sessionData.TempName).Append("*\n")
              .Append("📞 Телефон: *").Append(sessionData.TempPhone).Append("*\n")
              .Append("👨‍🔧 Мастер: *").Append(master?.FirstName).Append(' ').Append(master?.LastName).Append("*\n")
              .Append("📅 Дата: *").Append(sessionData.SelectedDateTime.Value.ToString("dd.MM.yyyy")).Append("*\n")
              .Append("🕐 Время: *").Append(sessionData.SelectedDateTime.Value.ToString("HH:mm")).Append("*\n\n")
              .Append("💅 *Услуги:*\n");

            foreach (var service in services)
            {
                sb.Append("  • ").Append(service.Name)
                  .Append(" - ").Append(service.DurationMinutes)
                  .Append(" мин, ").Append(service.Price.ToString("F2"))
                  .Append("₽\n");
            }

            sb.Append("\n⏱ *Общая длительность:* ").Append(totalDuration).Append(" мин")
              .Append("\n💰 *Сумма:* ").Append(totalPrice.ToString("F2")).Append("₽");

            if (!string.IsNullOrEmpty(sessionData.ClientComment))
            {
                sb.Append("\n\n📝 *Комментарий:* ").Append(sessionData.ClientComment);
            }

            sb.Append("\n\n✅ *Нажмите кнопку, чтобы подтвердить запись!*");

            var text = sb.ToString();

            await _bot.SendMessage(
                chatId,
                text,
                parseMode: ParseMode.Markdown,
                replyMarkup: KeyboardService.GetBookingKeyboard(totalPrice));
        }
        private async Task SendInvalidPhoneMessage(long chatId)
        {
            await _bot.SendMessage(chatId,
                                   "⚠️ Некорректный номер телефона. Попробуйте еще раз:",
                                   replyMarkup: KeyboardService.GetCancelKeyboard());
        }

        private async Task ShowPhoneActions(long chatId)
        {
            var sessionData = await _sessionService.GetSessionData(chatId);

            var text = $"✅ *Данные сохранены!*\n\n" +
                       $"👤 Имя: *{sessionData.TempName}*\n" +
                       $"📞 Телефон: *{sessionData.TempPhone}*\n\n" +
                       $"Выберите действие:";

            await _bot.SendMessage(
                chatId,
                text,
                parseMode: ParseMode.Markdown,
                replyMarkup: KeyboardService.GetPhoneActionsKeyboard());
        }


    }
}
