namespace ManicureStudio.Bot.DTOs
{
    public class TelegramUserUpdateDto
    {
        public string? CurrentStep { get; set; }
        public string? SessionData { get; set; }
        public int? ClientId { get; set; }
        public int? MasterId { get; set; }
    }
}
