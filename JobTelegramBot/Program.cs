using HeadhunterWebApi;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var botClient = new TelegramBotClient("5025298700:AAHNaRm-AhyS7xdJrODZNORIX1U5TanMZY0");

using var cts = new CancellationTokenSource();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
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

async Task HandleUpdateAsync(
    ITelegramBotClient botClient,
    Update update,
    CancellationToken cancellationToken)
{
    // Only process Message updates: https://core.telegram.org/bots/api#message
    // if (update.Message is not { } message)
    //     return;
    if (update.Message is null)
        return;
    Message message = update.Message;

    // Only process text messages
    if (message.Text is not { } messageText)
        return;

    long chatId = message.Chat.Id;

    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

    switch (messageText)
    {
        case "/start":
        {
            await HandleStart();
            break;
        }
        case "/all_employers":
        {
            await HandleAllEmployers();
            break;
        }
        default:
        {
            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"Привет, {messageText}!",
                cancellationToken: cancellationToken);
            break;
        }
    }

    Task HandleStart()
    {
        return botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Пожалуйста, представьтесь!",
            cancellationToken: cancellationToken);
    }

    async Task HandleAllEmployers()
    {
        var client = new HeadhunterClient();
        EmployersResponse employers = await client.GetEmployersAsync(null);
        _ = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: employers.ToString(),
            cancellationToken: cancellationToken);
    }
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