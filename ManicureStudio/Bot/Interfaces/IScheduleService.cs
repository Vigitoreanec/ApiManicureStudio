
namespace ManicureStudio.Bot.Interfaces
{
    public interface IScheduleService
    {
        Task<List<DateTime>> GetAvailableDates(int value, DateTime today);
        Task<List<DateTime>> GetAvailableTimeSlots(int value, DateTime date, int serviceDuration);
        Task<bool> IsTimeSlotAvailable(int value, DateTime startTime, DateTime endTime);
    }
}
