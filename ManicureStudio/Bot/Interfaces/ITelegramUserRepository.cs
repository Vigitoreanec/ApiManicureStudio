using ManicureStudio.Bot.DTOs;
using ManicureStudio.Core.Entities;

namespace ManicureStudio.Bot.Repositories
{
    public interface ITelegramUserRepository
    {
        Task<TelegramUser?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default);
        Task<TelegramUser> GetOrCreateAsync(TelegramUserCreateDto dto, CancellationToken cancellationToken = default);
        Task UpdateStateAsync(long telegramId, TelegramUserUpdateDto dto, CancellationToken cancellationToken = default);
        Task UpdateUserAsync(TelegramUser user, CancellationToken cancellationToken = default);
        Task LinkToClientAsync(long telegramId, int clientId, CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
