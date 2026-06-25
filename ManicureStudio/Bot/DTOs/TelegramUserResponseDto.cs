namespace ManicureStudio.Bot.DTOs
{
    public class TelegramUserResponseDto
    {
        public long TelegramId { get; set; }
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int? ClientId { get; set; }
        public int? MasterId { get; set; }
        public string? CurrentStep { get; set; }
        public DateTime LastActivityAt { get; set; }
    }
}
