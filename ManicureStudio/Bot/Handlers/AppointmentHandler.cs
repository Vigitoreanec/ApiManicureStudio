using ManicureStudio.Bot.Infrastructure;
using ManicureStudio.Bot.Interfaces;
using ManicureStudio.Core.Entities;
using ManicureStudio.Core.Enums;
using ManicureStudio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ManicureStudio.Bot.Handlers
{
    public class AppointmentHandler(ITelegramBotClient bot,
                                    AppDbContext context,
                                    ISessionService sessionService,
                                    IScheduleService scheduleService,
                                    ClientHandler clientHandler,
                                    ILogger<AppointmentHandler> logger)
    {
        private readonly ITelegramBotClient _bot = bot;
        private readonly AppDbContext _context = context;
        private readonly ISessionService _sessionService = sessionService;
        private readonly IScheduleService _scheduleService = scheduleService;
        private readonly ClientHandler _clientHandler = clientHandler;
        private readonly ILogger<AppointmentHandler> _logger = logger;


        public async Task ShowMasters(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;
            var messageId = callback.Message.MessageId;

            var masters = await _context.Masters
                .Where(m => !m.IsDeleted && m.IsActive)
                .OrderBy(m => m.FirstName)
                .ToListAsync();

            if (masters.Count == 0)
            {
                await _bot.SendMessage(
                    chatId,
                    "😔 К сожалению, мастера временно недоступны.",
                    replyMarkup: KeyboardService.GetMainMenuKeyboard());
                return;
            }

            await _sessionService.UpdateStep(chatId, "select_master");

            var sessionData = await _sessionService.GetSessionData(chatId);
            var serviceDuration = sessionData.SelectedServiceIds.Any()
                ? await _context.Services
                    .Where(s => sessionData.SelectedServiceIds.Contains(s.Id))
                    .SumAsync(s => s.DurationMinutes)
                : 30;

            await _bot.EditMessageText(
                chatId,
                messageId,
                $"👨‍🔧 *Выберите мастера:*\n\n" +
                $"Длительность сеанса: {serviceDuration} минут",
                replyMarkup: KeyboardService.GetMastersKeyboard(masters),
                parseMode: ParseMode.Markdown);
        }

        public async Task HandleMasterSelection(CallbackQuery callback, int masterId)
        {
            var chatId = callback.Message.Chat.Id;

            await _sessionService.UpdateSessionData(chatId, data =>
            {
                data.SelectedMasterId = masterId;
            });

            // даты
            await ShowDates(callback);
        }

        public async Task ShowDates(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;
            var messageId = callback.Message.MessageId;
            var sessionData = await _sessionService.GetSessionData(chatId);

            if (!sessionData.SelectedMasterId.HasValue)
            {
                await ShowMasters(callback);
                return;
            }

            await _sessionService.UpdateStep(chatId, "select_date");

            var availableDates = await _scheduleService.GetAvailableDates(
                sessionData.SelectedMasterId.Value,
                DateTime.Today);

            if (availableDates.Count == 0)
            {
                await _bot.EditMessageText(
                    chatId,
                    messageId,
                    "😔 На ближайшие дни нет свободных окон.\n\n" +
                    "Попробуйте выбрать другого мастера или дату.",
                    replyMarkup: KeyboardService.GetBackToMasterKeyboard());
                return;
            }

            await _bot.EditMessageText(
                chatId,
                messageId,
                $"📅 *Выберите дату:*\n\n" +
                $"Мастер: {await GetMasterName(sessionData.SelectedMasterId.Value)}",
                replyMarkup: KeyboardService.GetDatesKeyboard(availableDates),
                parseMode: ParseMode.Markdown);
        }

        public async Task HandleDateSelection(CallbackQuery callback, DateTime date)
        {
            var chatId = callback.Message.Chat.Id;
            var sessionData = await _sessionService.GetSessionData(chatId);

            await _sessionService.UpdateSessionData(chatId, data =>
            {
                data.SelectedDateTime = date;
                data.TimePage = 0;
            });

            await ShowTimeSlots(callback);
        }

        public async Task HandleTimePage(CallbackQuery callback, int page)
        {
            var chatId = callback.Message.Chat.Id;

            await _sessionService.UpdateSessionData(chatId, data =>
            {
                data.TimePage = page;
            });

            await ShowTimeSlots(callback);
        }

        public async Task HandlePaymentSelection(CallbackQuery callback, string paymentMethod)
        {
            var chatId = callback.Message.Chat.Id;

            var payment = paymentMethod.ToLower() switch
            {
                "card" => PaymentMethod.Card,
                "cash" => PaymentMethod.Cash,
                "prepayment" => PaymentMethod.Prepayment,
                _ => PaymentMethod.Cash
            };

            await _sessionService.UpdateSessionData(chatId, data =>
            {
                data.PaymentMethod = payment;
            });

            await _bot.EditMessageText(chatId,
                                       callback.Message.MessageId,
                                       $"✅ Способ оплаты выбран: *{payment}*\n\n" +
                                        "Теперь введите ваши данные для записи:",
                                       replyMarkup: KeyboardService.GetPaymentKeyboard(),
                                       parseMode: ParseMode.Markdown);

            await _clientHandler.RequestClientName(callback);
        }
        public async Task ShowTimeSlots(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;
            var messageId = callback.Message.MessageId;
            var sessionData = await _sessionService.GetSessionData(chatId);

            if (!sessionData.SelectedMasterId.HasValue || !sessionData.SelectedDateTime.HasValue)
            {
                await ShowDates(callback);
                return;
            }

            var serviceDuration = await GetTotalDuration(sessionData.SelectedServiceIds);

            var slots = await _scheduleService.GetAvailableTimeSlots(
                sessionData.SelectedMasterId.Value,
                sessionData.SelectedDateTime.Value.Date,
                serviceDuration);

            if (slots.Count == 0)
            {
                await _bot.EditMessageText(
                    chatId,
                    messageId,
                    "😔 На эту дату нет свободных окон.\n\n" +
                    "Выберите другую дату:",
                    replyMarkup: KeyboardService.GetDatesKeyboard(await GetAvailableDates(sessionData.SelectedMasterId.Value)));
                return;
            }

            await _sessionService.UpdateStep(chatId, "select_time");

            await _bot.EditMessageText(
                chatId,
                messageId,
                $"🕐 *Выберите время:*\n\n" +
                $"Дата: {sessionData.SelectedDateTime:dd.MM.yyyy}\n" +
                $"Длительность: {serviceDuration} мин",
                replyMarkup: KeyboardService.GetTimeSlotsKeyboard(slots, sessionData.TimePage),
                parseMode: ParseMode.Markdown);
        }

        public async Task HandleTimeSelection(CallbackQuery callback, TimeSpan time)
        {
            var chatId = callback.Message.Chat.Id;
            var sessionData = await _sessionService.GetSessionData(chatId);
            // ✅ ЛОГ: проверяем что пришло
            _logger.LogInformation("🕐 Получено время: {Time}", time);

            // ✅ ЛОГ: проверяем дату из сессии
            _logger.LogInformation("📅 Дата из сессии: {Date}", sessionData.SelectedDateTime);

            // Объединяем дату и время
            var selectedDateTime = sessionData.SelectedDateTime.Value.Date + time;
                

            await _sessionService.UpdateSessionData(chatId, data =>
            {
                data.SelectedDateTime = selectedDateTime;
            });

            // Запрашиваем имя клиента (Блок 15)
            await _clientHandler.RequestClientName(callback);
        }

        public async Task<Appointment> CreateAppointment(long chatId)
        {
            var sessionData = await _sessionService.GetSessionData(chatId);
            var user = await _sessionService.GetUser(chatId);

            // Получаем или создаем клиента (Блок 18)
            var client = await _clientHandler.RegisterClient(chatId);

            // Проверяем слот (Блок 20-21)
            var serviceDuration = await GetTotalDuration(sessionData.SelectedServiceIds);
            var startTime = sessionData.SelectedDateTime.Value;
            var endTime = startTime.AddMinutes(serviceDuration);

            var isAvailable = await _scheduleService.IsTimeSlotAvailable(
                sessionData.SelectedMasterId.Value,
                startTime,
                endTime);

            if (!isAvailable)
            {
                throw new Exception("❌ Слот времени уже занят. Пожалуйста, выберите другое время.");
            }

            // Создаем запись (Блок 23)
            var appointment = new Appointment
            {
                ClientId = client.Id,
                MasterId = sessionData.SelectedMasterId.Value,
                StartTime = startTime,
                EndTime = endTime,
                Status = AppointmentStatus.Confirmed,
                ClientComment = sessionData.ClientComment,
                PaymentMethod = sessionData.PaymentMethod ?? PaymentMethod.Cash,
                IsPaid = sessionData.IsPaid,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _context.Appointments.AddAsync(appointment);
            await _context.SaveChangesAsync();

            // Связываем услуги с записью
            foreach (var serviceId in sessionData.SelectedServiceIds)
            {
                var appointmentService = new AppointmentService
                {
                    AppointmentId = appointment.Id,
                    ServiceId = serviceId
                };
                await _context.AppointmentServices.AddAsync(appointmentService);
            }
            await _context.SaveChangesAsync();

            _logger.LogInformation("Создана запись #{AppointmentId} для клиента {ClientId} к мастеру {MasterId}",
                appointment.Id, client.Id, sessionData.SelectedMasterId.Value);

            return appointment;
        }

        public async Task ConfirmBooking(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;
            var messageId = callback.Message.MessageId;

            try
            {
                /*await _bot.AnswerCallbackQuery(callback.Id, "⏳ Обработка записи...");*/
                // Создаем запись
                var appointment = await CreateAppointment(chatId);

                // Получаем данные для уведомления
                var master = await _context.Masters
                    .FirstOrDefaultAsync(m => m.Id == appointment.MasterId);

                var services = await _context.Services
                    .Where(s => appointment.AppointmentServices.Select(a => a.ServiceId).Contains(s.Id))
                    .ToListAsync();

                // Уведомление клиенту (Блок 24)
                var clientText = $"✅ *Вы успешно записаны!* 🎉\n\n" +
                               $"📋 Номер записи: #{appointment.Id}\n" +
                               $"👨‍🔧 Мастер: {master?.FirstName} {master?.LastName}\n" +
                               $"📅 Дата: {appointment.StartTime:dd.MM.yyyy}\n" +
                               $"🕐 Время: {appointment.StartTime:HH:mm}\n" +
                               $"💅 Услуги: {string.Join(", ", services.Select(s => s.Name))}\n" +
                               $"💰 Сумма: {services.Sum(s => s.Price):F2}₽\n\n" +
                               $"📝 Мы отправим напоминание за 24 часа до записи!";

                await _bot.EditMessageText(
                    chatId,
                    messageId,
                    clientText,
                    parseMode: ParseMode.Markdown,
                    replyMarkup: KeyboardService.GetMainMenuKeyboard());

                // Уведомление мастеру (Блок 24)
                await NotifyMaster(appointment, master!, services);

                // Очищаем сессию
                await _sessionService.ClearSession(chatId);

                _logger.LogInformation("✅ Запись #{AppointmentId} успешно создана для {ChatId}",
                                       appointment.Id,
                                       chatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка создания записи для {ChatId}", chatId);

                if (ex.Message.Contains("занят"))
                {
                    await _bot.EditMessageText(
                        chatId,
                        messageId,
                        $"❌ *Слот времени занят!*\n\n{ex.Message}\n\nПожалуйста, выберите другое время.",
                        parseMode: ParseMode.Markdown,
                        replyMarkup: KeyboardService.GetBackToTimeKeyboard());
                }
                else
                {
                    await _bot.EditMessageText(
                        chatId,
                        messageId,
                        $"❌ *Ошибка записи*\n\n{ex.Message}\n\nПопробуйте еще раз:",
                        parseMode: ParseMode.Markdown,
                        replyMarkup: KeyboardService.GetMainMenuKeyboard());
                }
            }
        }

        public async Task CancelAppointment(CallbackQuery callback, int appointmentId)
        {
            var chatId = callback.Message.Chat.Id;

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId && !a.IsDeleted);

            if (appointment == null)
            {
                await _bot.AnswerCallbackQuery(callback.Id, "Запись не найдена", true);
                return;
            }

            if (appointment.StartTime < DateTime.Now)
            {
                await _bot.AnswerCallbackQuery(callback.Id, "Нельзя отменить прошедшую запись", true);
                return;
            }

            // Показываем подтверждение отмены
            await _bot.EditMessageText(
                chatId,
                callback.Message.MessageId,
                $"⚠️ *Вы уверены, что хотите отменить запись #{appointmentId}?*\n\n" +
                $"Дата: {appointment.StartTime:dd.MM.yyyy HH:mm}",
                parseMode: ParseMode.Markdown,
                replyMarkup: KeyboardService.GetCancelConfirmationKeyboard(appointmentId));
        }

        public async Task ConfirmCancel(CallbackQuery callback, int appointmentId)
        {
            var chatId = callback.Message.Chat.Id;

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId && !a.IsDeleted);

            if (appointment == null)
            {
                await _bot.AnswerCallbackQuery(callback.Id, "Запись не найдена", true);
                return;
            }

            appointment.IsDeleted = true;
            appointment.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Запись #{AppointmentId} отменена пользователем {ChatId}", appointmentId, chatId);

            await _bot.EditMessageText(
                chatId,
                callback.Message.MessageId,
                $"✅ *Запись #{appointmentId} успешно отменена*\n\n" +
                $"Дата: {appointment.StartTime:dd.MM.yyyy HH:mm}",
                parseMode: ParseMode.Markdown,
                replyMarkup: KeyboardService.GetMainMenuKeyboard());
        }

        public async Task NotifyMaster(Appointment appointment, Master master, List<Service> services)
        {
            try
            {
                if (master == null)
                    return;
                var client = await _context.Clients
                            .FirstOrDefaultAsync(c => c.Id == appointment.ClientId && !c.IsDeleted);

                var totalPrice = services.Sum(s => s.Price);
                var serviceNames = string.Join(", ", services.Select(s => s.Name));

                var sb = new StringBuilder();
                sb.Append("📅 *Новая запись!*\n\n")
                  .Append("👤 Клиент: ").Append(appointment.Client.FirstName).Append(' ').Append(appointment.Client.LastName).Append('\n')
                  .Append("📞 Телефон: ").Append(appointment.Client.PhoneNumber).Append('\n')
                  .Append("📅 Дата: ").Append(appointment.StartTime.ToString("dd.MM.yyyy")).Append('\n')
                  .Append("🕐 Время: ").Append(appointment.StartTime.ToString("HH:mm")).Append('\n')
                  .Append("💅 Услуги: ").Append(string.Join(", ", services.Select(s => s.Name))).Append('\n')
                  .Append("💰 Сумма: ").Append(services.Sum(s => s.Price).ToString("F2")).Append("₽\n");

                if (!string.IsNullOrEmpty(appointment.ClientComment))
                {
                    sb.Append("📝 Комментарий: ").Append(appointment.ClientComment);
                }

                var text = sb.ToString();

                await _bot.SendMessage(
                    master.Id,
                    text,
                    parseMode: ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка уведомления мастера {MasterId}", appointment.MasterId);
            }
        }

        private async Task<int> GetTotalDuration(List<int> serviceIds)
        {
            return await _context.Services
                .Where(s => serviceIds.Contains(s.Id))
                .SumAsync(s => s.DurationMinutes);
        }

        private async Task<string> GetMasterName(int masterId)
        {
            var master = await _context.Masters
                .FirstOrDefaultAsync(m => m.Id == masterId);
            return master != null ? $"{master.FirstName} {master.LastName}" : "Неизвестный мастер";
        }

        private async Task<List<DateTime>> GetAvailableDates(int masterId)
        {
            return await _scheduleService.GetAvailableDates(masterId, DateTime.Today);

        }
    }
}
