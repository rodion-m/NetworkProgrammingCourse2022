using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace FileDownloader;

public class StreamProducerConsumer
{
    private readonly int _readBufferSize;
    private readonly Channel<byte[]> _channel;
    private readonly ArrayPool<byte> _arrayPool;
    private long _producingStarted;
    private long _producingCompleted;
    private long _consumingStarted;
    private static readonly ConfiguredAsyncDisposable s_disposableDummy 
        = new DisposableDummy().ConfigureAwait(false);
    private const int ReportConsumingProgressEveryMs = 50;
    private const int ReportProducingProgressEveryMs = 50;

    public StreamProducerConsumer(int capacity, int readBufferSize)
    {
        _readBufferSize = readBufferSize;
        _channel = Channel.CreateBounded<byte[]>(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                //AllowSynchronousContinuations = true,
                SingleReader = true,
                SingleWriter = true,
            });
        _arrayPool = ArrayPool<byte>.Create(readBufferSize, 50);
    }

    public Task Start(
        Stream producingStream,
        Stream consumingStream,
        bool closeProducingStream = true,
        bool closeConsumingStream = true,
        CancellationToken cancellationToken = default,
        IProgress<long>? producingProgress = null,
        IProgress<long>? consumingProgress = null)
    {
        ThrowIfProducingCompleted();
        ThrowIfProducingStarted();
        ThrowIfConsumingStarted();
        cancellationToken.ThrowIfCancellationRequested();
        return Task.WhenAll(
            StartProducing(producingStream, closeProducingStream, cancellationToken, producingProgress),
            StartConsuming(consumingStream, closeConsumingStream, cancellationToken, consumingProgress)
        );
    }
    public async Task StartProducing(
        Stream stream,
        bool closeStream,
        CancellationToken cancellationToken,
        IProgress<long>? progress)
    {
        ThrowIfProducingCompleted();
        ThrowIfProducingStarted();
        Interlocked.Exchange(ref _producingStarted, 1);
        await using (closeStream ? stream.ConfigureAwait(false) : s_disposableDummy)
        {
            int bytesRead;
            long totalBytesRead = 0;
            using var rent = MemoryPool<byte>.Shared.Rent(_readBufferSize);
            var buffer = rent.Memory;
            Stopwatch? sw = null;
            if (progress is not null)
            {
                sw = Stopwatch.StartNew();
            }
            do
            {
                bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                var copy = PooledCopy(buffer, bytesRead);
                if (bytesRead > 0)
                {
                    await _channel.Writer.WriteAsync(copy, cancellationToken).ConfigureAwait(false);
                    totalBytesRead += copy.Length;
                    if (progress is not null && sw?.ElapsedMilliseconds >= ReportProducingProgressEveryMs)
                    {
                        sw.Restart();
                        progress.Report(totalBytesRead);
                    }
                }
            } while (bytesRead > 0);
            progress?.Report(totalBytesRead);
        }

        Interlocked.Exchange(ref _producingCompleted, 1);
        _channel.Writer.Complete();
        Interlocked.Exchange(ref _producingStarted, 0);
    }

    public async Task StartConsuming(
        Stream stream,
        bool closeStream,
        CancellationToken cancellationToken,
        IProgress<long>? progress)
    {
        ThrowIfProducingCompleted();
        ThrowIfConsumingStarted();
        Interlocked.Exchange(ref _consumingStarted, 1);
        Stopwatch? sw = null;
        if (progress is not null)
        {
            sw = Stopwatch.StartNew();
        }
        await using (closeStream ? stream.ConfigureAwait(false) : s_disposableDummy)
        {
            long totalBytesWritten = 0;
            var items = _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false);
            await foreach (var bytes in items)
            {
                await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                if (bytes.Length == _readBufferSize)
                {
                    _arrayPool.Return(bytes);
                }

                totalBytesWritten += bytes.LongLength;
                if (progress is not null && sw?.ElapsedMilliseconds >= ReportConsumingProgressEveryMs)
                {
                    sw.Restart();
                    progress.Report(totalBytesWritten);
                }
            }
            progress?.Report(totalBytesWritten);
        }
        Interlocked.Exchange(ref _consumingStarted, 0);
    }
    
    private byte[] PooledCopy(Memory<byte> buffer, int count)
    {
        byte[] copy;
        if (count == buffer.Length)
        {
            copy = _arrayPool.Rent(buffer.Length);
            if (copy.Length != count)
            {
                copy = new byte[count];
            }
            buffer.CopyTo(copy);
        }
        else
        {
            copy = buffer[..count].ToArray();
        }

        return copy;
    }

    private void ThrowIfProducingStarted()
    {
        if (Interlocked.Read(ref _producingStarted) == 1)
        {
            throw new InvalidOperationException("Producing is already started. " +
                                                $"{nameof(StreamProducerConsumer)} can have only one producer.");
        }
    }

    private void ThrowIfProducingCompleted()
    {
        if (Interlocked.Read(ref _producingCompleted) == 1)
        {
            throw new InvalidOperationException("Producing is already completed. " +
                                                $"It's impossible to use {nameof(StreamProducerConsumer)} more than once.");
        }
    }
    
    private void ThrowIfConsumingStarted()
    {
        if (Interlocked.Read(ref _consumingStarted) == 1)
        {
            throw new InvalidOperationException("Consuming is already started. " +
                                                $"{nameof(StreamProducerConsumer)} can have only one consumer.");
        }
    }
    
    private class DisposableDummy : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
