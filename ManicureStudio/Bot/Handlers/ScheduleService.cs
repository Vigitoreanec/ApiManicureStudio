using ManicureStudio.Bot.Interfaces;
using ManicureStudio.Core.Entities;
using ManicureStudio.Core.Enums;
using ManicureStudio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ManicureStudio.Bot.Handlers
{
    public class ScheduleService(AppDbContext context,
                                 IConfiguration configuration,
                                 ILogger<ScheduleService> logger) : IScheduleService
    {
        private readonly AppDbContext _context = context;
        private readonly IConfiguration _configuration = configuration;
        private readonly ILogger<ScheduleService> _logger = logger;

        private readonly int _workStartHour = configuration.GetValue<int>("BotSettings:WorkStartHour", 9);
        private readonly int _workEndHour = configuration.GetValue<int>("BotSettings:WorkEndHour", 21);
        private readonly int _slotDurationMinutes = configuration.GetValue<int>("BotSettings:SlotDurationMinutes", 30);

        public async Task<List<DateTime>> GetAvailableDates(int masterId, DateTime fromDate)
        {
            var dates = new List<DateTime>();
            var maxDays = 30; // Показываем на месяц вперед

            for (int i = 0; i < maxDays; i++)
            {
                var date = fromDate.AddDays(i);

                // Пропускаем выходные
                if (date.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                // Проверяем, есть ли свободные слоты
                var slots = await GetAvailableTimeSlots(masterId, date, 30);
                if (slots.Count != 0)
                {
                    dates.Add(date);
                }
            }

            return dates;
        }

        public async Task<List<DateTime>> GetAvailableTimeSlots(int masterId,
                                                                DateTime date,
                                                                int durationMinutes = 30)
        {
            var slots = new List<DateTime>();
            var startTime = date.Date.AddHours(_workStartHour);
            var endTime = date.Date.AddHours(_workEndHour);

            // Получаем все существующие записи за день
            var existingAppointments = await _context.Appointments
                .Where(a => a.MasterId == masterId &&
                           a.StartTime.Date == date.Date &&
                           !a.IsDeleted &&
                           a.Status != AppointmentStatus.Completed)
                .ToListAsync();

            var currentSlot = startTime;

            while (currentSlot.AddMinutes(durationMinutes) <= endTime)
            {
                var isAvailable = true;
                var slotEnd = currentSlot.AddMinutes(durationMinutes);

                foreach (var appointment in existingAppointments)
                {
                    if (currentSlot < appointment.EndTime &&
                        slotEnd > appointment.StartTime)
                    {
                        isAvailable = false;
                        break;
                    }
                }

                if (isAvailable)
                {
                    slots.Add(currentSlot);
                }

                currentSlot = currentSlot.AddMinutes(_slotDurationMinutes);
            }

            return slots;
        }

        public async Task<bool> IsTimeSlotAvailable(int masterId,
                                                    DateTime startTime,
                                                    DateTime endTime)
        {
            var existingAppointments = await _context.Appointments
                .Where(a => a.MasterId == masterId &&
                           a.StartTime.Date == startTime.Date &&
                           !a.IsDeleted &&
                           a.Status != AppointmentStatus.Completed)
                .ToListAsync();

            foreach (var appointment in existingAppointments)
            {
                if (startTime < appointment.EndTime &&
                    endTime > appointment.StartTime)
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<List<Appointment>> GetClientAppointments(int clientId)
        {
            return await _context.Appointments
                .Include(a => a.Master)
                .Include(a => a.AppointmentServices)
                .ThenInclude(ap => ap.Service)
                .Where(a => a.ClientId == clientId && !a.IsDeleted)
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();
        }

        public async Task<Appointment?> GetAppointmentById(int appointmentId)
        {
            return await _context.Appointments
                .Include(a => a.Client)
                .Include(a => a.Master)
                .Include(a => a.AppointmentServices)
                .ThenInclude(ap => ap.Service)
                .FirstOrDefaultAsync(a => a.Id == appointmentId && !a.IsDeleted);
        }

        public async Task<bool> CancelAppointment(int appointmentId)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId && !a.IsDeleted);

            if (appointment == null)
                return false;

            appointment.IsDeleted = true;
            appointment.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Запись #{AppointmentId} отменена ", appointmentId);

            return true;
        }
    }
}
