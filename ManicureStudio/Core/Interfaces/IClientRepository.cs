using ManicureStudio.Core.Entities;

namespace ManicureStudio.Core.Interfaces
{
    public interface IClientRepository:IRepository<Client>
    {
        Task<Client?> GetByPhoneAsync(string phoneNumber);
        Task<IEnumerable<Client>> GetClientsWithAppointmentsAsync(DateTime from, DateTime to);
        Task<IEnumerable<Client>> GetVipClientsAsync();
    }
}
