using Telegram.Bot.Types;

namespace ManicureStudio.Bot.Interfaces
{
    public interface IBotService
    {
        //start
        Task HandleStart(Message message);
        Task HandleTextMessage(Message message);
        Task HandleNonTextMessage(Message message);
        Task HandleCallback(CallbackQuery callback);
        Task SendErrorMessage(long chatId, string message);
    }
}
