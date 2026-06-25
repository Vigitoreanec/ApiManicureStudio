namespace ManicureStudio.Bot.Exceptions
{
    public class BotException : Exception
    {
        public string UserMessage { get; set; }
        public bool InfoAdmin { get; set; }

        public BotException(string message, string userMessage = "Произошла ошибка. Попробуйте позже.", 
                            bool infoAdmin = true)
        : base(message)
        {
            UserMessage = userMessage;
            InfoAdmin = infoAdmin;
        }

        public class ValidationException : BotException
        {
            public ValidationException(string message)
                : base(message, message, false) { }
        }

        public class NotFoundException : BotException
        {
            public NotFoundException(string entity, object id)
                : base($"{entity} с идентификатором {id} не найден", "Запись не найдена", false) { }
        }

        public class ConflictException : BotException
        {
            public ConflictException(string message)
                : base(message, message, false) { }
        }

    }
}
