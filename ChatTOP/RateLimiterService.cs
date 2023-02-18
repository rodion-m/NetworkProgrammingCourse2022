using System.Collections.Concurrent;
using System.Diagnostics.Contracts;

namespace ChatTOP;

/// <summary>
/// Provides rate limiting for Telegram Bot API
/// </summary>
/// <remarks>This class is thread-safe</remarks>
public class RateLimiterService : IRateLimiterService
{
    private readonly ConcurrentDictionary<long, Queue<DateTime>> _userIdToRequestTimes =
        new();
    
    private readonly RateLimitInfo _rateLimitInfo;

    public RateLimiterService(RateLimitInfo rateLimitInfo)
    {
        _rateLimitInfo = rateLimitInfo ?? throw new ArgumentNullException(nameof(rateLimitInfo));
    }

    /// <summary>
    /// Возвращает true, если пользователь превысил лимит запросов
    /// </summary>
    public bool ShouldRateLimit(long userId, out TimeSpan timeToWait)
    {
        var userRequests = _userIdToRequestTimes.GetOrAdd(
            userId, _ => new Queue<DateTime>());
        lock (userRequests)
        {
            var now = DateTime.UtcNow;
            return ShouldRateLimit(userRequests, now, out timeToWait);
        }
    }

    internal bool ShouldRateLimit(Queue<DateTime> userRequests, DateTime now, out TimeSpan timeToWait)
    {
        if (IsRateLimited(userRequests, now, out timeToWait))
        {
            return true;
        }
        if (userRequests.Count == _rateLimitInfo.RequestsCount)
        {
            // В случае если время не истекло, но очередь заполнена, удаляем первый элемент
            userRequests.Dequeue();
        }
        userRequests.Enqueue(now);
        return false;
    }

    [Pure]
    internal bool IsRateLimited(Queue<DateTime> userRequests, DateTime now, out TimeSpan timeToWait)
    {
        if (userRequests.Count == _rateLimitInfo.RequestsCount)
        {
            userRequests.TryPeek(out var firstRequestTime);
            timeToWait = GetTimeToWait(now, firstRequestTime);
            return timeToWait > TimeSpan.Zero;
        }
        timeToWait = TimeSpan.Zero;
        return false;
    }
    
    [Pure]
    internal TimeSpan GetTimeToWait(DateTime now, DateTime firstRequestTime)
    {
        var timeElapsedSinceFirstRequest = now - firstRequestTime;
        if (timeElapsedSinceFirstRequest < _rateLimitInfo.Interval)
        {
            return _rateLimitInfo.Interval - timeElapsedSinceFirstRequest;
        }
        return TimeSpan.Zero;
    }
}