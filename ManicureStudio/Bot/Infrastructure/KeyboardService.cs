using ManicureStudio.Core.Entities;
using Telegram.Bot.Types.ReplyMarkups;

namespace ManicureStudio.Bot.Infrastructure
{
    public static class KeyboardService
    {
        // меню
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
                InlineKeyboardButton.WithCallbackData("👤 Профиль", "profile")
            ]
        ]);
        }
        // категории
        public static InlineKeyboardMarkup GetCategoriesKeyboard(List<ServiceCategory> categories)
        {
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var category in categories)
            {
                buttons.Add([InlineKeyboardButton.WithCallbackData(
                    $"📁 {category.Name}",
                    $"cat_{category.Id}"
                    )
                ]);
            }

            buttons.Add([InlineKeyboardButton.WithCallbackData("🏠 В меню", "main_menu")]);

            return new InlineKeyboardMarkup(buttons);
        }
        // услуги
        public static InlineKeyboardMarkup GetServicesKeyboard(List<Service> services,
                                                               List<int>? selectedIds = null)
        {
            var buttons = new List<List<InlineKeyboardButton>>();
            selectedIds ??= [];

            foreach (var service in services)
            {
                var isSelected = selectedIds.Contains(service.Id);
                var prefix = isSelected ? "✅" : "💅";

                buttons.Add([
                        InlineKeyboardButton.WithCallbackData(
                        $"{prefix} {service.Name} - {service.Price:F2}₽",
                        $"serv_{service.Id}")
                        ]);

            }

            // Кнопки управления
            var navButtons = new List<InlineKeyboardButton>();

            if (selectedIds.Count != 0)
            {
                var totalPrice = services.Where(s => selectedIds.Contains(s.Id)).Sum(s => s.Price);
                                navButtons.Add(InlineKeyboardButton.WithCallbackData(
                                $"✅ Готово ({totalPrice:F2}₽)",
                                "confirm"));
            }
            else
            {
                navButtons.Add(InlineKeyboardButton.WithCallbackData(
                    "❌ Очистить",
                    "clear"));
            }

            if (navButtons.Count != 0)
            {
                buttons.Add(navButtons);
            }

            buttons.Add([
                    InlineKeyboardButton.WithCallbackData("🔙 Назад", "back"),
                    InlineKeyboardButton.WithCallbackData("🏠 Меню", "main_menu")
                    ]);

            return new InlineKeyboardMarkup(buttons);
        }
        // дата
        public static InlineKeyboardMarkup GetDatesKeyboard(List<DateTime> availableDates)
        {
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var date in availableDates.Take(14)) // Показываем 14 дней
            {
                var dayNames = new Dictionary<DayOfWeek, string>
            {
                { DayOfWeek.Monday, "ПН" },
                { DayOfWeek.Tuesday, "ВТ" },
                { DayOfWeek.Wednesday, "СР" },
                { DayOfWeek.Thursday, "ЧТ" },
                { DayOfWeek.Friday, "ПТ" },
                { DayOfWeek.Saturday, "СБ" }
            };

                buttons.Add([
                    InlineKeyboardButton.WithCallbackData(
                    $"{dayNames[date.DayOfWeek]} {date:dd.MM}",
                    $"date_{date:yyyy-MM-dd}")]);
                
            }

            buttons.Add([
                InlineKeyboardButton.WithCallbackData("🔙 Назад", "back"),
                InlineKeyboardButton.WithCallbackData("🏠 Меню", "main_menu")
                ]);

            return new InlineKeyboardMarkup(buttons);
        }
        // время
        public static InlineKeyboardMarkup GetTimeSlotsKeyboard(
        List<DateTime> slots,
        int page = 0)
        {
            var buttons = new List<List<InlineKeyboardButton>>();
            var slotsPerPage = 8;
            var startIndex = page * slotsPerPage;
            var endIndex = Math.Min(startIndex + slotsPerPage, slots.Count);

            for (int i = startIndex; i < endIndex; i++)
            {
                var slot = slots[i];
                buttons.Add([
                    InlineKeyboardButton.WithCallbackData(
                    $"{slot:HH:mm}",
                    $"time_{slot:yyyy-MM-dd_HH:mm}")]
                    );
            }

            // Пагинация
            var navButtons = new List<InlineKeyboardButton>();
            if (page > 0)
                navButtons.Add(InlineKeyboardButton.WithCallbackData("◀️", $"page_{page - 1}"));
            if (endIndex < slots.Count)
                navButtons.Add(InlineKeyboardButton.WithCallbackData("▶️", $"page_{page + 1}"));

            if (navButtons.Count != 0)
                buttons.Add(navButtons);

            buttons.Add([
                InlineKeyboardButton.WithCallbackData("🔙 Назад", "back"),
                InlineKeyboardButton.WithCallbackData("🏠 Меню", "main_menu")]
            );

            return new InlineKeyboardMarkup(buttons);
        }
        // оплата
        public static InlineKeyboardMarkup GetPaymentKeyboard()
        {
            return new InlineKeyboardMarkup(
            [
                [
                InlineKeyboardButton.WithCallbackData("💳 Карта", "pay_card"),
                InlineKeyboardButton.WithCallbackData("💵 Наличные", "pay_cash"),
                InlineKeyboardButton.WithCallbackData("📱 Онлайн", "pay_online")
                ],
                [
                InlineKeyboardButton.WithCallbackData("🔙 Назад", "back")
                ]
            ]);
        }
        // тел
        public static InlineKeyboardMarkup GetPhoneActionsKeyboard()
        {
            return new InlineKeyboardMarkup(
            [
            [
                InlineKeyboardButton.WithCallbackData($"✅ Записаться", "book_without_comment")
            ],
            [
                InlineKeyboardButton.WithCallbackData("✏️ Добавить комментарий", "add_comment"),
                InlineKeyboardButton.WithCallbackData("🔙 Назад", "back_to_phone")
            ]
        ]);
        }
        // ком
        public static InlineKeyboardMarkup GetCommentKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("⏭️ Пропустить", "skip_comment"),
            /*InlineKeyboardButton.WithCallbackData("✅ Записаться", "book_confirm")*/
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("🔙 Назад", "back")
        }
    });
        }
        // записи
        public static InlineKeyboardMarkup GetCancelConfirmationKeyboard(int appointmentId)
        {
            return new InlineKeyboardMarkup(
            [
            [
                InlineKeyboardButton.WithCallbackData("✅ Да, отменить", $"confirm_cancel_{appointmentId}")
            ],
            [
                InlineKeyboardButton.WithCallbackData("🔙 Нет, вернуться", "back")
            ]
        ]);
        }
        // мастера
        public static InlineKeyboardMarkup GetMastersKeyboard(List<Master> masters)
        {
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var master in masters)
            {
                buttons.Add([
                    InlineKeyboardButton.WithCallbackData(
                    $"👨‍🔧 {master.FirstName} {master.LastName}",
                    $"master_{master.Id}")
                ]);
                
            }

            buttons.Add([InlineKeyboardButton.WithCallbackData("🔙 Назад", "back")]);
            
            return new InlineKeyboardMarkup(buttons);
        }

        public static InlineKeyboardMarkup GetBackToMasterKeyboard()
        {
            return new InlineKeyboardMarkup(
            [
            [InlineKeyboardButton.WithCallbackData("🔙 К мастерам", "back")]
        ]);
        }

        public static InlineKeyboardMarkup GetBackToMenuKeyboard()
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
        // подт
        public static InlineKeyboardMarkup GetBookingKeyboard(decimal totalPrice)
        {
            return new InlineKeyboardMarkup(new[]
            {
        new[]
        {
            InlineKeyboardButton.WithCallbackData($"✅ Подтвердить запись ({totalPrice:F2}₽)", "book_confirm")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("🔙 Назад", "back"),
            InlineKeyboardButton.WithCallbackData("❌ Отменить", "cancel")
        }
    });
        }

        public static InlineKeyboardMarkup GetBackToTimeKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("🕐 Выбрать другое время", "back")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("🏠 В меню", "main_menu")
        }
    });
        }
    }
}
