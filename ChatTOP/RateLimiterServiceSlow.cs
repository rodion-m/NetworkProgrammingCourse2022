using System.Collections.Concurrent;

namespace ChatTOP;

/// <summary>
/// Provides rate limiting for Telegram Bot API
/// </summary>
/// <remarks>This class is NOT fully thread-safe</remarks>
public class RateLimiterServiceSlow : IRateLimiterService
{
    private readonly ConcurrentDictionary<long /*userId*/, List<DateTime>> _userIdToRequestTimes =
        new();
    
    private readonly RateLimitInfo _rateLimitInfo;

    public RateLimiterServiceSlow(RateLimitInfo rateLimitInfo)
    {
        _rateLimitInfo = rateLimitInfo ?? throw new ArgumentNullException(nameof(rateLimitInfo));
    }

    /// <summary>
    /// Возвращает true, если пользователь превысил лимит запросов
    /// </summary>
    public bool ShouldRateLimit(long userId, out TimeSpan timeToWait)
    {
        var userRequests = _userIdToRequestTimes.GetOrAdd(
            userId, _ => new List<DateTime>());
        var now = DateTime.UtcNow;
        //userRequests.RemoveAll(requestDateTime => !IsRecentRequest(requestDateTime));
        var recentRequests = userRequests.Where(IsRecentRequest).Take(10).ToArray();
        if (recentRequests.Length >= _rateLimitInfo.RequestsCount)
        {
            timeToWait = _rateLimitInfo.Interval - (now - recentRequests[0]);
            return true;
        }
        userRequests.Add(now);
        timeToWait = TimeSpan.Zero;
        return false;

        bool IsRecentRequest(DateTime requestTime) 
            => now - requestTime < _rateLimitInfo.Interval;
    }
}