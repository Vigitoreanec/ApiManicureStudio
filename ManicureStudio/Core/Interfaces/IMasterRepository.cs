using ManicureStudio.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ManicureStudio.Core.Interfaces
{
    public interface IMasterRepository : IRepository<Master>
    {
        Task<IEnumerable<Master>> GetActiveMastersWithServicesAsync();
        Task<IEnumerable<Master>> GetMastersByServiceAsync(int serviceId);
        Task<string?> GetServiceNameByIdAsync(int serviceId);
        
        Task<bool> IsMasterAvailableAsync
            (int masterId, DateTime startTime, DateTime endTime, int? excludeAppointmentId = null);
    }
}
