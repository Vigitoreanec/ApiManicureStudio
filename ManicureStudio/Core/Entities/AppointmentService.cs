namespace ManicureStudio.Core.Entities
{
    public class AppointmentService
    {
        public int AppointmentId { get; set; }
        public int ServiceId { get; set; }
        public decimal PriceAtBooking { get; set; } //Цена на момент записи
        public Appointment? Appointment { get; set; }
        public Service? Service { get; set; }
    }
}
