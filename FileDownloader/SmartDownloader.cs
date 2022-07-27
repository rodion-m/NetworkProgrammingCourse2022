namespace FileDownloader;

public class SmartDownloader
{
    private static readonly Lazy<HttpClient> s_lazyHttpClient = new(() => new HttpClient());
    private const int BufferSize = 16384;
    private const int _1GB = 1024 * 1024 * 1024;
    private const int MaxQueueSize = _1GB / BufferSize;

    /// <summary>
    /// Downloads and saves file in separate threads (first thread - downloading, second thread - saving)
    /// </summary>
    public static async Task DownloadFileAsync(
        string uri,
        string outputPath,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default,
        IProgress<LoadingProgress>? progress = null)
    {
        if (uri == null) throw new ArgumentNullException(nameof(uri));
        if (outputPath == null) throw new ArgumentNullException(nameof(outputPath));
        cancellationToken.ThrowIfCancellationRequested();
        httpClient ??= s_lazyHttpClient.Value;

        const HttpCompletionOption option = HttpCompletionOption.ResponseHeadersRead;
        using var response = await httpClient.GetAsync(uri, option, cancellationToken).ConfigureAwait(false);
        var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        var contentLength = response.Content.Headers.ContentLength;
        var fileStream = File.OpenWrite(outputPath);
        var producerConsumer = new StreamProducerConsumer(MaxQueueSize, BufferSize);
        var consumingProgress = await ConvertProgress();
        await producerConsumer.Start(
            contentStream,
            fileStream,
            cancellationToken: cancellationToken,
            consumingProgress: consumingProgress
        );

        Task<Progress<long>?> ConvertProgress()
        {
            if (progress is null) return Task.FromResult((Progress<long>?)null);
            return Task.Run(
                () =>
                {
                    return (Progress<long>?)new Progress<long>(savedBytes =>
                    {
                        progress.Report(new LoadingProgress(contentLength, savedBytes));
                    });
                }, cancellationToken);
        }
    }

    public readonly record struct LoadingProgress(long? TotalSize, long DownloadedBytes)
    {
        public float? Progress => (float) DownloadedBytes / TotalSize;
    }
}