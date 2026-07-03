using ManicureStudio.Bot.Infrastructure;
using ManicureStudio.Bot.Interfaces;
using ManicureStudio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ManicureStudio.Bot.Handlers
{
    public class ProfileHandler(ITelegramBotClient bot,
                                AppDbContext context,
                                ISessionService sessionService,
                                ILogger<ProfileHandler> logger)
    {
        private readonly ITelegramBotClient _bot = bot;
        private readonly AppDbContext _context = context;
        private readonly ISessionService _sessionService = sessionService;
        private readonly ILogger<ProfileHandler> _logger = logger;

        public async Task ShowProfile(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;
            var user = await _sessionService.GetUser(chatId);

            if (user == null || !user.ClientId.HasValue)
            {
                await _bot.EditMessageText(
                    chatId,
                    callback.Message.MessageId,
                    "👤 *Профиль*\n\n" +
                    "У вас еще нет профиля. Запишитесь на услугу, и мы создадим его!",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: KeyboardService.GetBackToMenuKeyboard());
                return;
            }

            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Id == user.ClientId && !c.IsDeleted);

            if (client == null)
            {
                await _bot.EditMessageText(
                    chatId,
                    callback.Message.MessageId,
                    "👤 *Профиль*\n\n" +
                    "Ваш профиль не найден в системе.",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: KeyboardService.GetMainMenuKeyboard());
                return;
            }

            var appointments = await _context.Appointments
                .Where(a => a.ClientId == client.Id && !a.IsDeleted)
                .OrderByDescending(a => a.StartTime)
                .Take(5)
                .ToListAsync();

            var text = $"👤 *Профиль*\n\n" +
                      $"Имя: {client.FirstName} {client.LastName}\n" +
                      $"Телефон: {client.PhoneNumber}\n" +
                      $"Всего записей: {appointments.Count}\n\n";

            if (appointments.Count != 0)
            {
                text += "*Последние записи:*\n";
                foreach (var app in appointments.Take(3))
                {
                    text += $"• {app.StartTime:dd.MM.yyyy HH:mm} - " +
                           $"{app.Status}\n";
                }
            }

            await _bot.EditMessageText(
                chatId,
                callback.Message.MessageId,
                text,
                parseMode: ParseMode.Markdown,
                replyMarkup: KeyboardService.GetMainMenuKeyboard());
        }

        public async Task ShowBookings(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;
            var user = await _sessionService.GetUser(chatId);

            if (user == null || !user.ClientId.HasValue)
            {
                await _bot.EditMessageText(
                    chatId,
                    callback.Message.MessageId,
                    "📅 *Мои записи*\n\n" +
                    "У вас пока нет записей.\n" +
                    "Хотите записаться?",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                    new[] { InlineKeyboardButton.WithCallbackData("📋 Записаться", "menu_services") },
                    new[] { InlineKeyboardButton.WithCallbackData("🏠 В меню", "main_menu") }
                    }));
                return;
            }

            var appointments = await _context.Appointments
                .Include(a => a.Master)
                .Include(a => a.AppointmentServices)
                .ThenInclude(ap => ap.Service)
                .Where(a => a.ClientId == user.ClientId && !a.IsDeleted)
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();

            if (!appointments.Any())
            {
                await _bot.EditMessageText(
                    chatId,
                    callback.Message.MessageId,
                    "📅 *Мои записи*\n\n" +
                    "У вас пока нет записей.",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: KeyboardService.GetMainMenuKeyboard());
                return;
            }

            var activeAppointments = appointments
                .Where(a => a.StartTime >= DateTime.Now)
                .ToList();

            var pastAppointments = appointments
                .Where(a => a.StartTime < DateTime.Now)
                .ToList();

            var text = "📅 *Мои записи*\n\n";

            if (activeAppointments.Count != 0)
            {
                text += "*Активные записи:*\n";
                foreach (var app in activeAppointments)
                {
                    var services = app.AppointmentServices.Select(ap => ap.Service.Name);
                    text += $"• {app.StartTime:dd.MM.yyyy HH:mm} - " +
                           $"{app.Master?.FirstName} {app.Master?.LastName}\n" +
                           $"  Услуги: {string.Join(", ", services)}\n" +
                           $"  Статус: {app.Status}\n\n";
                }
            }

            if (pastAppointments.Count != 0)
            {
                text += "*Прошедшие записи:*\n";
                foreach (var app in pastAppointments.Take(3))
                {
                    text += $"• {app.StartTime:dd.MM.yyyy HH:mm} - " +
                           $"{app.Status}\n";
                }
            }

            await _bot.EditMessageText(
                chatId,
                callback.Message.MessageId,
                text,
                parseMode: ParseMode.Markdown,
                replyMarkup: KeyboardService.GetMainMenuKeyboard());
        }
    }
}
