using ManicureStudio.API.APIResult;
using System;
using System.Net;
using System.Text.Json;

namespace ManicureStudio.API.Middleware
{
    public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<ExceptionMiddleware> _logger = logger;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Передаём управление следующему middleware в цепочке
                await _next(context);
            }
            catch (Exception ex)
            {
                // Если что-то пошло не так — обрабатываем здесь
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            // Определяем тип ошибки и HTTP-статус
            var (statusCode, message) = ex switch
            {
                // Ресурс не найден
                KeyNotFoundException => (HttpStatusCode.NotFound, ex.Message),

                // Ошибка валидации данных
                ArgumentException => (HttpStatusCode.BadRequest, ex.Message),

                // Доступ запрещён
                UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Доступ запрещён"),

                // Все прочие ошибки — внутренняя ошибка сервера
                _ => (HttpStatusCode.InternalServerError, "Произошла внутренняя ошибка сервера")
            };

            // Логируем ошибку 
            if (statusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(ex,
                    "Необработанное исключение: {ExceptionType} | Path: {Path}",
                    ex.GetType().Name,
                    context.Request.Path);
            }
            else
            {
                _logger.LogWarning(ex,
                    "Ожидаемое исключение: {ExceptionType} | {Message}",
                    ex.GetType().Name,
                    ex.Message);
            }

            //context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = ApiResult<object>.Failure(message);

            // Сериализуем в JSON
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }
}
