namespace ManicureStudio.Core.Entities
{
    public class ServiceCategory : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public int SortOrder { get; set; } = 0;
        public ICollection<Service> Services { get; set; } = new List<Service>();
    }
}
