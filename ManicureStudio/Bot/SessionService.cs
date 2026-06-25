using ManicureStudio.Bot.DTOs;
using ManicureStudio.Bot.Interfaces;
using ManicureStudio.Bot.Repositories;
using ManicureStudio.Core.Entities;

namespace ManicureStudio.Bot
{
    public class SessionService(ITelegramUserRepository userRepository, ILogger<SessionService> logger) : ISessionService
    {
        private readonly ITelegramUserRepository _userRepository = userRepository;
        private readonly ILogger<SessionService> _logger = logger;

        
        public async Task<TelegramUser> GetOrCreateUser(long telegramId,
                                                  string? username = null,
                                                  string? firstName = null,
                                                  string? lastName = null)
        {
            var dto = new TelegramUserCreateDto
            {
                TelegramId = telegramId,
                Username = username,
                FirstName = firstName,
                LastName = lastName
            };

            return await _userRepository.GetOrCreateAsync(dto);
        }

        public async Task<BotSession> GetSessionData(long telegramId)
        {
            var user = await _userRepository.GetByTelegramIdAsync(telegramId);
            if (user == null)
                return new BotSession();

            return BotSession.FromJson(user.SessionData);
        }

        public async Task<TelegramUser?> GetUser(long telegramId)
        {
            return await _userRepository.GetByTelegramIdAsync(telegramId);
        }
        public async Task ClearSession(long telegramId)
        {
            var user = await _userRepository.GetByTelegramIdAsync(telegramId);
            if (user == null)
                return;

            await _userRepository.UpdateStateAsync(telegramId, new TelegramUserUpdateDto
            {
                CurrentStep = "main_menu",
                SessionData = null
            });
        }


        public async Task LinkToClient(long telegramId, int clientId)
        {
            await _userRepository.LinkToClientAsync(telegramId, clientId);
        }

        public async Task UpdateSessionData(long telegramId, BotSession data)
        {
            await _userRepository.UpdateStateAsync(telegramId, new TelegramUserUpdateDto
            {
                SessionData = data.ToJson()
            });
        }

        public async Task UpdateSessionData(long telegramId, Action<BotSession> updateAction)
        {
            var data = await GetSessionData(telegramId);
            updateAction(data);
            await UpdateSessionData(telegramId, data);
        }

        public async Task UpdateStep(long telegramId, string step)
        {
            await _userRepository.UpdateStateAsync(telegramId, new TelegramUserUpdateDto
            {
                CurrentStep = step
            });
        }
    }
}
