using ManicureStudio.Bot.DTOs;
using ManicureStudio.Core.Entities;

namespace ManicureStudio.Bot.Interfaces
{
    public interface ISessionService
    {
        Task<TelegramUser> GetOrCreateUser(long telegramId,
                                           string? username = null,
                                           string? firstName = null,
                                           string? lastName = null);
        Task UpdateStep(long telegramId, string step);
        Task<BotSession> GetSessionData(long telegramId);
        Task UpdateSessionData(long telegramId, BotSession data);
        Task UpdateSessionData(long telegramId, Action<BotSession> updateAction);
        Task ClearSession(long telegramId);
        Task<TelegramUser?> GetUser(long telegramId);
        Task LinkToClient(long telegramId, int clientId);
    }
}
