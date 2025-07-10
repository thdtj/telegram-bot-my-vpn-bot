using System; using System.Collections.Concurrent; using System.Collections.Generic; using System.IO; using System.Net.Http; using System.Threading; using System.Threading.Tasks; using Telegram.Bot; using Telegram.Bot.Polling; using Telegram.Bot.Types; using Telegram.Bot.Types.Enums; using Telegram.Bot.Types.ReplyMarkups;

class Program { static string userBotToken = "7699584421:AAEJwX-zwh1pK9v4jFGQlXOL8NKzsPS9xro"; static string adminBotToken = "8097601891:AAFBoNMDTbpA_ee0AwRM3vS-1p5_YGCuGao";

static TelegramBotClient botUser = new(userBotToken);
static TelegramBotClient botAdmin = new(adminBotToken);

static HashSet<long> adminChatIds = new();
static Dictionary<long, long> pendingConfigs = new();
static ConcurrentDictionary<long, string> userStates = new();
static string latestTestConfig = "";
static HashSet<long> waitingTestConfigAdmins = new();

static async Task Main()
{
    var userCts = new CancellationTokenSource();
    var adminCts = new CancellationTokenSource();

    botUser.StartReceiving(HandleUserUpdate, HandleError, new ReceiverOptions(), userCts.Token);
    botAdmin.StartReceiving(HandleAdminUpdate, HandleError, new ReceiverOptions(), adminCts.Token);

    Console.WriteLine("ربات‌ها اجرا شدند...");
    await Task.Delay(-1);
}

static async Task HandleUserUpdate(ITelegramBotClient bot, Update update, CancellationToken token)
{
    if (update.Type == UpdateType.Message && update.Message.Type == MessageType.Contact)
    {
        var userId = update.Message.Chat.Id;
        var contact = update.Message.Contact;
        var username = update.Message.From?.Username ?? "بدون یوزرنیم";

        await bot.SendTextMessageAsync(userId, "✅ شماره با موفقیت ثبت شد.");

        foreach (var adminId in adminChatIds)
        {
            await botAdmin.SendTextMessageAsync(
                chatId: adminId,
                text: $"📞 کاربر جدید:\n👤 یوزرنیم: @{username}\n📱 شماره: {contact.PhoneNumber}\n🆔 ID: {userId}"
            );
        }

        var menu = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl("🛠 پشتیبانی", "https://t.me/amir_SH_1384_AB") },
            new[] { InlineKeyboardButton.WithCallbackData("💰 خرید", "buy") },
            new[] { InlineKeyboardButton.WithCallbackData("📤 ارسال رسید", "send_receipt") },
            new[] { InlineKeyboardButton.WithCallbackData("🧪 دریافت کانفیگ تست", "get_test_config") }
        });

        await bot.SendTextMessageAsync(userId, "لطفاً یکی از گزینه‌های زیر را انتخاب کنید:", replyMarkup: menu);
    }
    else if (update.Type == UpdateType.CallbackQuery)
    {
        var callback = update.CallbackQuery;
        var userId = callback.From.Id;

        if (callback.Data == "buy")
        {
            string msg = """

💳 تعرفه‌ها: 20 گیگ 130❌90 35 گیگ 175❌130 50 گیگ 215❌175 70 گیگ 260❌200 100 گیگ 330❌250 نامحدود 2 کاربره 420❌290 نامحدود 4 کاربره 450❌330 پنل خانوادگی 700❌480

💵 مبلغ را به شماره کارت ... واریز و رسید را ارسال کنید. """; await bot.SendTextMessageAsync(userId, msg); } else if (callback.Data == "send_receipt") { userStates[userId] = "awaiting_photo"; await bot.SendTextMessageAsync(userId, "📷 لطفاً عکس رسید را ارسال کنید."); } else if (callback.Data == "get_test_config") { if (!string.IsNullOrWhiteSpace(latestTestConfig)) await bot.SendTextMessageAsync(userId, $"🧪 کانفیگ تست:\n\n{latestTestConfig}"); else await bot.SendTextMessageAsync(userId, "❌ هنوز هیچ کانفیگ تستی ثبت نشده است."); }

await bot.AnswerCallbackQueryAsync(callback.Id);
    }
    else if (update.Type == UpdateType.Message && update.Message.Photo != null)
    {
        var user = update.Message.From;
        var chatId = update.Message.Chat.Id;

        if (userStates.TryGetValue(chatId, out var state) && state == "awaiting_photo")
        {
            var photo = update.Message.Photo[^1];
            var file = await botUser.GetFileAsync(photo.FileId);
            var filePath = file.FilePath;

            using var httpClient = new HttpClient();
            using var stream = await httpClient.GetStreamAsync($"https://api.telegram.org/file/bot{userBotToken}/{filePath}");

            foreach (var adminId in adminChatIds)
            {
                await botAdmin.SendPhotoAsync(
                    chatId: adminId,
                    photo: InputFile.FromStream(stream, "receipt.jpg"),
                    caption: $"🧾 رسید پرداخت از کاربر:\n👤 @{user?.Username ?? "بدون یوزرنیم"}\n🆔 ID: {user?.Id}",
                    replyMarkup: new InlineKeyboardMarkup(
                        InlineKeyboardButton.WithCallbackData("✅ تایید", $"ok_{chatId}")
                    )
                );
            }

            await bot.SendTextMessageAsync(chatId, "✅ رسید شما دریافت شد و در حال بررسی است.");
            userStates.TryRemove(chatId, out _);
        }
        else
        {
            await bot.SendTextMessageAsync(chatId, "برای ارسال رسید ابتدا روی دکمه «📤 ارسال رسید» کلیک کنید.");
        }
    }
    else if (update.Type == UpdateType.Message && update.Message.Text == "/start")
    {
        var requestContactKeyboard = new ReplyKeyboardMarkup(new[]
        {
            KeyboardButton.WithRequestContact("📞 ارسال شماره")
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };

        await bot.SendTextMessageAsync(update.Message.Chat.Id, "سلام، لطفاً شماره موبایل خود را ارسال کنید:", replyMarkup: requestContactKeyboard);
    }
}

static async Task HandleAdminUpdate(ITelegramBotClient bot, Update update, CancellationToken token)
{
    if (update.Type == UpdateType.Message && update.Message.Text == "/start")
    {
        var adminId = update.Message.Chat.Id;
        if (adminChatIds.Add(adminId))
        {
            Console.WriteLine($"✅ ادمین جدید اضافه شد: {adminId}");
            await bot.SendTextMessageAsync(adminId, "ربات ادمین آماده دریافت پیام‌ها است.", replyMarkup:
                new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("🧪 ایجاد کانفیگ تست", "create_test_config"))
            );
        }
    }
    else if (update.Type == UpdateType.CallbackQuery)
    {
        var callback = update.CallbackQuery;

        if (callback.Data.StartsWith("ok_"))
        {
            var userIdStr = callback.Data.Split('_')[1];
            if (long.TryParse(userIdStr, out long userId))
            {
                pendingConfigs[callback.From.Id] = userId;

                await botUser.SendTextMessageAsync(userId, "✅ پرداخت شما تایید شد.\n⌛ منتظر دریافت کانفیگ از طرف پشتیبانی باشید.");
                await bot.AnswerCallbackQueryAsync(callback.Id, "🕓 تایید شد. حالا کانفیگ را برای کاربر بفرست.");
                await bot.SendTextMessageAsync(callback.From.Id, $"✍️ لطفاً حالا کانفیگ را به صورت متن برای کاربر {userId} ارسال کن.");
            }
        }
        else if (callback.Data == "create_test_config")
        {
            waitingTestConfigAdmins.Add(callback.From.Id);
            await bot.AnswerCallbackQueryAsync(callback.Id);
            await bot.SendTextMessageAsync(callback.From.Id, "✍️ لطفاً کانفیگ تست را وارد کنید:");
        }
    }
    else if (update.Type == UpdateType.Message && !string.IsNullOrWhiteSpace(update.Message.Text))
    {
        var adminId = update.Message.Chat.Id;

        if (pendingConfigs.TryGetValue(adminId, out long targetUserId))
        {
            await botUser.SendTextMessageAsync(targetUserId, $"🌐 کانفیگ شما:\n\n{update.Message.Text}");
            await bot.SendTextMessageAsync(adminId, "✅ کانفیگ با موفقیت برای کاربر ارسال شد.");
            pendingConfigs.Remove(adminId);
        }
        else if (waitingTestConfigAdmins.Contains(adminId))
        {
            latestTestConfig = update.Message.Text;
            waitingTestConfigAdmins.Remove(adminId);
            await bot.SendTextMessageAsync(adminId, "✅ کانفیگ تست با موفقیت ذخیره شد.");
        }
    }
}

static Task HandleError(ITelegramBotClient bot, Exception exception, CancellationToken token)
{
    Console.WriteLine("❌ خطا: " + exception.Message);
    return Task.CompletedTask;
}

}

