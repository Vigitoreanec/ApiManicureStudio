namespace ManicureStudio.Core.Entities
{
    public class Master : BaseEntity
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Specialization { get; set; } = string.Empty; //Маникюр, Педикюр
        public string? Description { get; set; }
        public string? PhotoUrl { get; set; }
        public bool IsActive { get; set; } = true;

        //Список записей
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        //Услуги
        public ICollection<MasterService> MasterServices { get; set; } = new List<MasterService>();
    }
}
