namespace ManicureStudio.Bot.DTOs
{
    public class BotStatisticsResponse
    {
        public int TotalUsers { get; set; }
        public int ActiveUsersLast7Days { get; set; }
        public int TodayAppointments { get; set; }
        public int TotalAppointments { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
