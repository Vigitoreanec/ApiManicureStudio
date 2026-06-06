namespace ManicureStudio.API.APIResult
{
    public class ApiResult<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();

        public static ApiResult<T> Success(T data, string message = "Успешно")
        => new() { IsSuccess = true, Data = data, Message = message };
        public static ApiResult<T> Success(string message = "Успешно")
        => new() { IsSuccess = true, Message = message };
        public static ApiResult<T> Failure(string message, List<string>? errors = null)
        => new() { IsSuccess = false, Message = message, Errors = errors ?? new() };
        public static ApiResult<T> Failure(string message, string error)
        => new() { IsSuccess = false, Message = message, Errors = new() { error } };
    }

    public class ApiResult : ApiResult<object>
    {
        public static ApiResult Ok(string message = "Операция выполнена")
            => new() { IsSuccess = true, Message = message };

        public static new ApiResult Failure(string message, List<string>? errors = null)
            => new() { IsSuccess = false, Message = message, Errors = errors ?? new() };
    }
}
