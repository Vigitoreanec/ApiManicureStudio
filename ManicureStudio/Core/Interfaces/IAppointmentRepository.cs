using ManicureStudio.Core.Entities;

namespace ManicureStudio.Core.Interfaces
{
    public interface IAppointmentRepository : IRepository<Appointment>
    {
        Task<IEnumerable<Appointment>> GetMasterAppointmentsAsync(int masterId, DateOnly date);
        Task<IEnumerable<Appointment>> GetClientAppointmentsAsync(int clientId);
        Task<Appointment?> GetWithDetailsAsync(int appointmentId);
        Task<IEnumerable<Appointment>> GetUpcomingAppointmentsAsync(int hoursAhead = 24);
    }
}
