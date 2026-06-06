using Microsoft.OpenApi.Models;

namespace ManicureStudio.API
{
    public static class ApiServiceExtensions
    {
        public static IServiceCollection AddSwaggerDocumentation(
        this IServiceCollection services,
        IConfiguration configuration)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                var swagger = configuration.GetSection("SwaggerSettings");

                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = swagger["Title"] ?? "Manicure Studio API",
                    Description = swagger["Description"] ?? string.Empty,
                    Version = swagger["Version"] ?? "v1",
                    Contact = new OpenApiContact
                    {
                        Name = swagger["ContactName"],
                        Email = swagger["ContactEmail"]
                    }
                });

                // Поддержка JWT в Swagger UI — кнопка "Authorize"
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Введите JWT токен в формате: Bearer {token}"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id   = "Bearer"
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
                                 ?? Array.Empty<string>();

            services.AddCors(options =>
            {
                options.AddPolicy("ManicureStudioCors", policy =>
                {
                    policy
                        .WithOrigins(allowedOrigins)    // Только разрешённые источники
                        .AllowAnyHeader()                // Любые заголовки
                        .AllowAnyMethod()                // GET, POST, PUT, DELETE и т.д.
                        .AllowCredentials();             // Cookies / Authorization header
                });
            });

            return services;
        }
    }
}
