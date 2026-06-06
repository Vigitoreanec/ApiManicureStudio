namespace ManicureStudio.Core.Entities
{
    public class MasterService
    {
        public int MasterId { get; set; }
        public int ServiceId { get; set; }
        public decimal? CustomPrice { get; set; }

        public Master? Master { get; set; }
        public Service? Service { get; set; }
    }
}
