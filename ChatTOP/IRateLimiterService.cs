namespace ChatTOP;

public interface IRateLimiterService
{
    /// <summary>
    /// Возвращает true, если пользователь превысил лимит запросов
    /// </summary>
    bool ShouldRateLimit(long userId, out TimeSpan timeToWait);
}