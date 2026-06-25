namespace ManicureStudio.Bot.DTOs
{
    public class TelegramUserCreateDto
    {
        public long TelegramId { get; set; }
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
}
