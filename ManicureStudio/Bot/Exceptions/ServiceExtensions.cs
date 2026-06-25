using ManicureStudio.Bot.Interfaces;
using ManicureStudio.Bot.Repositories;
using Newtonsoft.Json.Linq;
using System.Net;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace ManicureStudio.Bot.Exceptions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddBotServices(this IServiceCollection services,
                                                        IConfiguration configuration)
        {
            var token = configuration["TelegramBot:Token"]
                ?? throw new InvalidOperationException("Telegram Bot Token не настроен");

            services.AddSingleton<ITelegramBotClient>(sp =>
            {
                var httpClientHandler = new HttpClientHandler
                {
                    UseProxy = true,
                    Proxy = WebRequest.GetSystemWebProxy()
                };

                // Если нужна авторизация через системные учетные данные
                httpClientHandler.Proxy.Credentials = CredentialCache.DefaultCredentials;

                var httpClient = new HttpClient(httpClientHandler);
                httpClient.Timeout = TimeSpan.FromMinutes(5);

                return new TelegramBotClient(token, httpClient);
            });
            /*services.AddSingleton<ITelegramBotClient>(sp => new TelegramBotClient(token));*/

            services.AddScoped<ITelegramUserRepository, TelegramUserRepository>();

            services.AddScoped<ISessionService, SessionService>();
            services.AddScoped<IBotService, BotService>();

            services.AddScoped<IUpdateHandler, UpdateHandler>();
            services.AddScoped<BotExceptionHandler>();

            services.AddHostedService<BotHostedService>();

            services.AddLogging(configure =>
            {
                configure.AddConsole();
                configure.AddDebug();
            });

            return services;
        }
    }

}
    

            