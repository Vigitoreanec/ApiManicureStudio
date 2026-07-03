using ManicureStudio.Core.Enums;
using Newtonsoft.Json;

namespace ManicureStudio.Bot.DTOs
{
    public class BotSession
    {
        public List<int> SelectedServiceIds { get; set; } = [];
        public int? SelectedCategoryId { get; set; }
        public int? SelectedServiceId { get; set; }

        public int? SelectedMasterId { get; set; }
        public DateTime? SelectedDateTime { get; set; }
        // TEMP
        public string? TempName { get; set; }
        public string? TempPhone { get; set; }
        // Доп данные
        public string? ClientComment { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public bool IsPaid { get; set; } = false;

        public int? EditingAppointmentId { get; set; }
        public DateTime SessionStartTime { get; set; } = DateTime.Now;
        public int? LastMessageId { get; set; }
        public int TimePage { get; set; } = 0;
        public string ToJson() => JsonConvert.SerializeObject(this);
        public static BotSession FromJson(string? json)
        {
            if (string.IsNullOrEmpty(json))
                return new BotSession();

            try
            {
                return JsonConvert.DeserializeObject<BotSession>(json)
                       ?? new BotSession();
            }
            catch
            {
                return new BotSession();
            }
        }
    }
}
