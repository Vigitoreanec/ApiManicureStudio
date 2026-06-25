namespace ManicureStudio.Core.Entities
{
    public class TelegramUser : BaseEntity
    {
        public long TelegramId { get; set; }
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int? ClientId { get; set; }
        public virtual Client? Client { get; set; }
        public int? MasterId { get; set; }
        public virtual Master? Master { get; set; }

        // Для хранения состояния диалога
        public string? CurrentStep { get; set; }

        public string? SessionData { get; set; }
        public DateTime LastActivityAt => UpdatedAt ?? CreatedAt;
    }
}
