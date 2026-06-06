using ManicureStudio.Core.Entities;
using ManicureStudio.Core.Interfaces;
using ManicureStudio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ManicureStudio.Infrastructure.Repositories
{
    public class ClientRepository(AppDbContext context) : Repository<Client>(context), IClientRepository
    {
        public async Task<Client?> GetByPhoneAsync(string phoneNumber)
        {
            return await _dbSet
                .FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
        }
        public async Task<IEnumerable<Client>> GetClientsWithAppointmentsAsync(DateTime from, DateTime to)
        {
            return await _dbSet
                .Include(c => c.Appointments
                    .Where(a => a.StartTime >= from && a.StartTime <= to))
                .Where(c => c.Appointments.Any(a => a.StartTime >= from && a.StartTime <= to))
                .ToListAsync();
        }
        public async Task<IEnumerable<Client>> GetVipClientsAsync()
        {
            return await _dbSet
                .Where(c => c.IsVip)
                .OrderBy(c => c.LastName)
                .ToListAsync();
        }
    }
}
