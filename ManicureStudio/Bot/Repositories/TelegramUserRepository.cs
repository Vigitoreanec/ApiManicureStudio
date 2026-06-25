using ManicureStudio.Bot.DTOs;
using ManicureStudio.Bot.Exceptions;
using ManicureStudio.Core.Entities;
using ManicureStudio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ManicureStudio.Bot.Repositories
{
    public class TelegramUserRepository(AppDbContext context, ILogger<TelegramUserRepository> logger) : ITelegramUserRepository
    {
        private readonly AppDbContext _context = context;
        private readonly ILogger<TelegramUserRepository> _logger = logger;

        public async Task<TelegramUser?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken)
        {
            return await _context.TelegramUsers
            .Include(u => u.Client)
            .Include(u => u.Master)
            .FirstOrDefaultAsync(u => u.TelegramId == telegramId, cancellationToken);
        }

        public async Task<TelegramUser> GetOrCreateAsync(TelegramUserCreateDto dto, CancellationToken cancellationToken)
        {
            var user = await GetByTelegramIdAsync(dto.TelegramId, cancellationToken);

            if (user != null)
            {
                // Обновляем данные, если изменились
                var hasChanges = false;

                if (!string.IsNullOrEmpty(dto.Username) && user.Username != dto.Username)
                {
                    user.Username = dto.Username;
                    hasChanges = true;
                }

                if (!string.IsNullOrEmpty(dto.FirstName) && user.FirstName != dto.FirstName)
                {
                    user.FirstName = dto.FirstName;
                    hasChanges = true;
                }

                if (!string.IsNullOrEmpty(dto.LastName) && user.LastName != dto.LastName)
                {
                    user.LastName = dto.LastName;
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    user.UpdatedAt = DateTime.Now;
                    await UpdateUserAsync(user, cancellationToken);
                }

                return user;
            }
            user = new TelegramUser
            {
                TelegramId = dto.TelegramId,
                Username = dto.Username,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                CurrentStep = "start",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _context.TelegramUsers.AddAsync(user, cancellationToken);
            await SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Создан новый пользователь Telegram: {TelegramId} ({Username})",
                user.TelegramId, user.Username ?? user.FirstName);

            return user;
        }

        public async Task LinkToClientAsync(long telegramId, int clientId, CancellationToken cancellationToken)
        {
            var user = await GetByTelegramIdAsync(telegramId, cancellationToken) ?? 
                        throw new BotException.NotFoundException(nameof(TelegramUser), telegramId);
            
            user.ClientId = clientId;
            await UpdateUserAsync(user, cancellationToken);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateStateAsync(long telegramId, TelegramUserUpdateDto dto, CancellationToken cancellationToken)
        {
            var user = await GetByTelegramIdAsync(telegramId, cancellationToken) ?? 
                        throw new BotException.NotFoundException(nameof(TelegramUser), telegramId);
           
            if (!string.IsNullOrEmpty(dto.CurrentStep))
                user.CurrentStep = dto.CurrentStep;

            if (dto.SessionData != null)
                user.SessionData = dto.SessionData;

            if (dto.ClientId.HasValue)
                user.ClientId = dto.ClientId.Value;

            if (dto.MasterId.HasValue)
                user.MasterId = dto.MasterId.Value;

            await UpdateUserAsync(user, cancellationToken);
        }

        public async Task UpdateUserAsync(TelegramUser user, CancellationToken cancellationToken)
        {
            user.UpdatedAt = DateTime.Now;
            _context.TelegramUsers.Update(user);
            await SaveChangesAsync(cancellationToken);
        }
    }
}
