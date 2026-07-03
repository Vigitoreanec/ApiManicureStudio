using Microsoft.OpenApi.Models;
using System.Reflection;


namespace ManicureStudio.API
{
    public static class ApiServiceExtensions
    {
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services,
                                                                 IConfiguration configuration)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Manicure Studio API",
                    Description = "API для управления приложением и Telegram ботом",
                    Version = "v1",
                    Contact = new OpenApiContact
                    {
                        Name = "ContactName" ?? string.Empty,
                        Email ="ContactEmail" ?? string.Empty,
                    }
                });

                // ===== XML комментарии =====
                try
                {
                    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    if (File.Exists(xmlPath))
                    {
                        options.IncludeXmlComments(xmlPath);
                    }
                }
                catch { }

                // ===== Secret Token для Telegram =====
                options.AddSecurityDefinition("SecretToken", new OpenApiSecurityScheme
                {
                    Name = "X-Telegram-Bot-Api-Secret-Token",
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Description = "Secret Token для верификации запросов от Telegram"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "SecretToken"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            return services;
        }


        /// <summary>
        /// Настраивает CORS — разрешает запросы с фронтенда.
        /// В продакшн замените на конкретные домены из конфигурации.
        /// </summary>
        public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
            var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>()
                                 ?? new[] { "http://localhost:3000", "http://localhost:5173" };


            services.AddCors(options =>
            {
                options.AddPolicy("ManicureStudioCors", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            return services;
        }
    }
}
