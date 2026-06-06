using ManicureStudio.Core.Enums;

namespace ManicureStudio.Core.Entities
{
    public class Appointment : BaseEntity
    {
        public int ClientId { get; set; }
        public int MasterId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
        public decimal TotalPrice { get; set; }
        public string? ClientComment { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public bool IsPaid { get; set; } = false;
        public Client? Client { get; set; }
        public Master? Master { get; set; }
        public ICollection<AppointmentService> AppointmentServices { get; set; } = new List<AppointmentService>();
    }
}
