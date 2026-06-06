using ManicureStudio.Core.Entities;
using ManicureStudio.Core.Interfaces;
using ManicureStudio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ManicureStudio.Infrastructure.Repositories
{
    public class MasterRepository(AppDbContext context) : Repository<Master>(context), IMasterRepository
    {
        public async Task<IEnumerable<Master>> GetActiveMastersWithServicesAsync()
        {
            return await _dbSet
                .Where(m => m.IsActive)
                .Include(m => m.MasterServices)
                    .ThenInclude(ms => ms.Service)
                .OrderBy(m => m.LastName)
                .ToListAsync();
        }
        public async Task<IEnumerable<Master>> GetMastersByServiceAsync(int serviceId)
        {
            return await _dbSet
                .Where(m => m.IsActive && m.MasterServices.Any(ms => ms.ServiceId == serviceId))
                .Include(m => m.MasterServices)
                    .ThenInclude(ms => ms.Service)
                .ToListAsync();
        }
        public async Task<bool> IsMasterAvailableAsync(
        int masterId,
        DateTime startTime,
        DateTime endTime,
        int? excludeAppointmentId = null)
        {
            // Проверяем: нет ли у мастера уже записи на это время
            var query = _context.Appointments
                .Where(a =>
                    a.MasterId == masterId &&
                    a.StartTime < endTime &&      // Существующая запись начинается до конца новой
                    a.EndTime > startTime &&       // Существующая запись заканчивается после начала новой
                    !a.IsDeleted);

            // Если обновляем существующую запись — исключаем её из проверки
            if (excludeAppointmentId.HasValue)
            {
                query = query.Where(a => a.Id != excludeAppointmentId.Value);
            }

            return !await query.AnyAsync(); // Свободен, если нет пересечений
        }
    }
}
