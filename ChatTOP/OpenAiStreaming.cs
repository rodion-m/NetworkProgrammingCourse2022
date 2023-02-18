using System.Text;
using System.Threading.Channels;
using OpenAI_API;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ChatTOP;

public class OpenAiStreaming
{
    private readonly ITelegramBotClient _botClient;
    private readonly OpenAIAPI _openAiClient;
    private readonly int _maxTokens;
    private readonly double _temperature;

    public OpenAiStreaming(ITelegramBotClient botClient, int maxTokens, double temperature)
    {
        if (maxTokens <= 0) throw new ArgumentOutOfRangeException(nameof(maxTokens));
        if (temperature is <= 0 or > 2) throw new ArgumentOutOfRangeException(nameof(temperature));
        _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
        _maxTokens = maxTokens;
        _temperature = temperature;
        _openAiClient = CreateOpenAiClientWithEnvironmentKey();
    }

    private OpenAIAPI CreateOpenAiClientWithEnvironmentKey()
    {
        string? key = Environment.GetEnvironmentVariable("openai_api_key");
        if (key == null)
        {
            throw new InvalidOperationException("Переменная окружения openai_api_key не задана!");
        }

        return new OpenAIAPI(key);
    }

    // Корявая реализация Producer-Consumer
    public async Task StreamTokensAndReceiveAsync(
        string prompt,
        long chatId,
        TimeSpan producingDelay,
        CancellationToken cancellationToken)
    {
        if (prompt == null) throw new ArgumentNullException(nameof(prompt));
        if (chatId <= 0) throw new ArgumentOutOfRangeException(nameof(chatId));
        if (producingDelay <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(producingDelay));

        Message? originalMessage = null;
        StringBuilder stringBuilder = new();
        var prevText = "";
        var sync = new object();
        bool streaming = true;
        var receivingTask = Task.Run(async () =>
        {
            while (streaming)
            {
                await TryUpdateMessage();
                await Task.Delay(producingDelay, cancellationToken);
            }

            await TryUpdateMessage();

            async Task TryUpdateMessage()
            {
                string text;
                lock (sync)
                {
                    text = stringBuilder.ToString();
                }

                if (!IsTextUpdated()) return;
                var isFirstChunk = originalMessage is null;
                if (isFirstChunk)
                {
                    originalMessage = await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: text,
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    await _botClient.EditMessageTextAsync(
                        messageId: originalMessage.MessageId,
                        chatId: originalMessage.Chat.Id,
                        text: text,
                        cancellationToken: cancellationToken
                    );
                }

                prevText = text;

                bool IsTextUpdated()
                {
                    if (string.IsNullOrWhiteSpace(text)) return false;
                    if (text.TrimEnd() == prevText.TrimEnd()) return false;
                    return true;
                }
            }
        });

        var stream = _openAiClient.Completions.StreamCompletionEnumerableAsync(
            prompt, max_tokens: _maxTokens, temperature: _temperature);
        await foreach (var token in stream
                           .WithCancellation(cancellationToken))
        {
            lock (sync)
            {
                stringBuilder.Append(token.ToString());
            }
        }

        streaming = false;
        await receivingTask;
    }

    // Нормальная реализация паттерна Producer(tokens)-Consumer-Producer(deepl)-Consumer(send telegram message) (Receiver)
    public Task ProduceAndConsumeResponseAsync(
        string prompt,
        long chatId,
        TimeSpan producingDelay,
        CancellationToken cancellationToken)
    {
        if (prompt == null) throw new ArgumentNullException(nameof(prompt));
        if (chatId <= 0) throw new ArgumentOutOfRangeException(nameof(chatId));
        if (producingDelay <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(producingDelay));

        //System.Threading.Channels, BlockingCollection
        var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(1000));

        var producingTask = Task.Run(ProduceTokensAsync);
        var consumingTask = ConsumeTokensAsync();
        return Task.WhenAll(producingTask, consumingTask);

        async Task ProduceTokensAsync()
        {
            var stream = _openAiClient.Completions
                .StreamCompletionEnumerableAsync(
                    prompt, max_tokens: _maxTokens, temperature: _temperature);
            await foreach (var token in stream
                               .WithCancellation(cancellationToken))
            {
                await channel.Writer.WriteAsync(token.ToString(), cancellationToken);
            }

            channel.Writer.Complete();
        }

        async Task ConsumeTokensAsync()
        {
            var reader = channel.Reader;
            var stringBuilder = new StringBuilder();
            string prevText = "";
            Message? originalMessage = null;
            while (await reader.WaitToReadAsync(cancellationToken))
            {
                while (reader.TryRead(out var token))
                {
                    stringBuilder.Append(token);
                }

                var text = stringBuilder.ToString();
                if (!IsTextUpdated()) return;
                var isFirstChunk = originalMessage is null;
                if (isFirstChunk)
                {
                    originalMessage = await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: text,
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    await _botClient.EditMessageTextAsync(
                        messageId: originalMessage!.MessageId,
                        chatId: originalMessage.Chat.Id,
                        text: text,
                        cancellationToken: cancellationToken
                    );
                }

                prevText = text;
                await Task.Delay(producingDelay, cancellationToken);

                bool IsTextUpdated()
                {
                    if (string.IsNullOrWhiteSpace(text)) return false;
                    if (text.TrimEnd() == prevText.TrimEnd()) return false;
                    return true;
                }
            }
        }
    }
}