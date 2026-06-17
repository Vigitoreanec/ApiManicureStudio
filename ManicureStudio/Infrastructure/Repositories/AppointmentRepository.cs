using ManicureStudio.Core.Entities;
using ManicureStudio.Core.Interfaces;
using ManicureStudio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ManicureStudio.Infrastructure.Repositories
{
    public class AppointmentRepository(AppDbContext context) : Repository<Appointment>(context), IAppointmentRepository
    {
        public async Task<IEnumerable<Appointment>> GetMasterAppointmentsAsync(int masterId, DateOnly date)
        {
            var startOfDay = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var endOfDay = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

            return await _dbSet
                .Where(a =>
                    a.MasterId == masterId &&
                    a.StartTime >= startOfDay &&
                    a.StartTime <= endOfDay)
                .Include(a => a.Client)
                .Include(a => a.AppointmentServices)
                    .ThenInclude(s => s.Service)
                .OrderBy(a => a.StartTime)
                .ToListAsync();
        }
        public async Task<IEnumerable<Appointment>> GetClientAppointmentsAsync(int clientId)
        {
            return await _dbSet
                .Where(a => a.ClientId == clientId)
                .Include(a => a.Master)
                .Include(a => a.AppointmentServices)
                    .ThenInclude(s => s.Service)
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();
        }
        public async Task<Appointment?> GetWithDetailsAsync(int appointmentId)
        {
            return await _dbSet
                .Include(a => a.Client)
                .Include(a => a.Master)
                .Include(a => a.AppointmentServices)
                    .ThenInclude(s => s.Service)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
        }
        public async Task<IEnumerable<Appointment>> GetUpcomingAppointmentsAsync(int hoursAhead = 24)
        {
            var now = DateTime.Now;
            var until = now.AddHours(hoursAhead);
            
            return await _dbSet
                .Where(a =>
                    a.StartTime >= now &&
                    a.StartTime <= until &&
                    a.Status == Core.Enums.AppointmentStatus.Confirmed)
                .Include(a => a.Client)
                .Include(a => a.Master)
                .OrderBy(a => a.StartTime)
                .ToListAsync();
        }
    }
}
