using ChatTOP;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var token = Environment.GetEnvironmentVariable("tg_chat_bot_2023_2");
if (token is null)
{
    throw new InvalidOperationException("Telegram bot token is not set in environment variable tg_chat_bot_2023_2");
}
var botClient = new TelegramBotClient(token)
{
    Timeout = TimeSpan.FromSeconds(10)
};
var openAiStreaming = new OpenAiStreaming(botClient, 300, 0.2);

using CancellationTokenSource cts = new();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
ReceiverOptions receiverOptions = new()
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

// Вызывается библиотекой Telegram.Bot.
async Task HandleUpdateAsync(
    ITelegramBotClient botClient,
    Update update,
    CancellationToken cancellationToken)
{
    // Only process Message updates: https://core.telegram.org/bots/api#message
    var message = update.Message;
    // Only process text messages
    if (message?.Text is not { } messageText)
        return;

    long chatId = message.Chat.Id;
    Console.WriteLine(messageText);
    await openAiStreaming.ProduceAndConsumeResponseAsync(
        messageText,
        chatId,
        TimeSpan.FromMilliseconds(500),
        cancellationToken
    );
}

Task HandlePollingErrorAsync(
    ITelegramBotClient botClient,
    Exception exception,
    CancellationToken cancellationToken)
{
    //Logger.LogError(exception, "Exception occurred while receiving updates");
    string? errorMessage;
    if (exception is ApiRequestException apiRequestException)
        errorMessage =
            $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}";
    else
        errorMessage = exception.ToString();

    Console.WriteLine(errorMessage);
    return Task.CompletedTask;
}