namespace ManicureStudio.Core.Entities
{
    public class Client : BaseEntity
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateOnly? BirthDate { get; set; }
        public string? Notes { get; set; }
        public bool IsVip { get; set; } = false;

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    }
}
