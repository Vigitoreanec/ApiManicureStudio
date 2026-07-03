namespace ManicureStudio.Bot.DTOs
{
    public class BotStatusResponse
    {
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string BotName { get; set; } = string.Empty;
        public string BotUsername { get; set; } = string.Empty;
        public long? BotId { get; set; }
        public bool IsWebhookSet { get; set; }
        public string? WebhookUrl { get; set; }
        public int PendingUpdates { get; set; }
    }
}
