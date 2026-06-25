using Telegram.Bot.Types.ReplyMarkups;

namespace ManicureStudio.Bot
{
    public static class KeyboardService
    {
        public static InlineKeyboardMarkup GetMainMenuKeyboard()
        {
            return new InlineKeyboardMarkup(
            [
            [
                InlineKeyboardButton.WithCallbackData("📋 Услуги", "menu_services")
            ],
            [
                InlineKeyboardButton.WithCallbackData("📅 Мои записи", "menu_bookings")
            ],
            [
                InlineKeyboardButton.WithCallbackData("👤 Профиль", "menu_profile")
            ]
        ]);
        }

        internal static InlineKeyboardMarkup? GetBackToMenuKeyboard()
        {
            return new InlineKeyboardMarkup(
            [
                [
                InlineKeyboardButton.WithCallbackData("🏠 В главное меню", "main_menu")
                ]
            ]);
        }

        public static InlineKeyboardMarkup GetCancelKeyboard()
        {
            return new InlineKeyboardMarkup(
            [
                [
                    InlineKeyboardButton.WithCallbackData("❌ Отменить", "cancel")
                ]
            ]);
        }
    }
}
