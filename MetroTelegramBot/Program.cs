using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

var botClient = new TelegramBotClient("YOUR_TOKEN");

using CancellationTokenSource cts = new();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    var handler = update.Type switch
    {
        UpdateType.Message => HandlerMessage(botClient, update, cancellationToken),
    };

    try
    {
        await handler;
    }
    catch (Exception ex)
    {
        await Console.Out.WriteLineAsync(ex.Message);
    }
}

async Task HandlerMessage(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Message.Location != null)
    {
        await HandlerLocationAsync(botClient, update, cancellationToken);

        //Message message = await botClient.SendTextMessageAsync()
    }
    else if (update.Message.Text != null)
    {
        ReplyKeyboardMarkup replyKeyboard = new ReplyKeyboardMarkup(
            KeyboardButton.WithRequestLocation("Send Location"));

        await botClient.SendTextMessageAsync(
            chatId: update.Message.Chat.Id,
            text: "Bizga Location jo'nating",
            replyMarkup: replyKeyboard,
            cancellationToken: cancellationToken);

    }

}

async Task HandlerLocationAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    var location = update.Message.Location;
    var lat = location.Latitude;
    var lon = location.Longitude;

    var NearLocation = await NearSubwayAsync(lat, lon);

    Message message = await botClient.SendLocationAsync(
        chatId: update.Message.Chat.Id,
        longitude: NearLocation.lon,
        latitude: NearLocation.lat,
        cancellationToken: cancellationToken
        );

}

async Task<SubwayLocation> NearSubwayAsync(double lat, double lon)
{
    string path = "C:\\Users\\iddiu\\OneDrive\\Рабочий стол\\Projects\\MetroTelegramBot\\MetroTelegramBot\\JsonLocations.json";

    var locationsJson = System.IO.File.ReadAllText(path);
    var result = JsonConvert.DeserializeObject<List<SubwayLocation>>(locationsJson);
    double min = double.MaxValue;

    SubwayLocation subwayLocation = new SubwayLocation();
    for (int i = 0; i < result.Count; i++)
    {
        if (Math.Sqrt(Math.Pow((result[i].lat - lat), 2) + Math.Pow((result[i].lon - lon), 2)) < min)
        {
            subwayLocation = result[i];
            min = Math.Sqrt(Math.Pow((result[i].lat - lat), 2) + Math.Pow((result[i].lon - lon), 2));
        }
    }
    return subwayLocation;
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}