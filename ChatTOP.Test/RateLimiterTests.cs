namespace ChatTOP.Test;

public class RateLimiterTests
{
    private readonly IRateLimiterService _rateLimiter;

    public RateLimiterTests()
    {
        // Set up RateLimiter for tests
        _rateLimiter = new RateLimiterService(new RateLimitInfo(5, TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public void ShouldRateLimit_ReturnsFalse_WhenRequestsBelowLimit()
    {
        // Arrange
        long userId = 1;
        TimeSpan timeToWait;

        // Act
        var isRateLimited = _rateLimiter.ShouldRateLimit(userId, out timeToWait);

        // Assert
        Assert.False(isRateLimited);
        Assert.Equal(TimeSpan.Zero, timeToWait);
    }

    [Fact]
    public void ShouldRateLimit_ReturnsTrue_WhenRequestsExceedLimit()
    {
        // Arrange
        long userId = 2;

        // Add requests to reach rate limit
        for (int i = 0; i < 5; i++)
        {
            _rateLimiter.ShouldRateLimit(userId, out _);
        }

        // Act
        var isRateLimited = _rateLimiter.ShouldRateLimit(userId, out var timeToWait);

        // Assert
        Assert.True(isRateLimited);
        Assert.Equal(10, Math.Round(timeToWait.TotalSeconds));
    }

    [Fact]
    public void ShouldRateLimit_ReturnsTrue_WhenRequestsExceedLimitAndOldRequestsRemoved()
    {
        // Arrange
        long userId = 3;

        // Add requests to reach rate limit
        for (int i = 0; i < 5; i++)
        {
            _rateLimiter.ShouldRateLimit(userId, out _);
        }

        // Wait for rate limit to expire
        Thread.Sleep(TimeSpan.FromSeconds(10));

        // Add more requests to ensure old requests are removed
        for (int i = 0; i < 2; i++)
        {
            _rateLimiter.ShouldRateLimit(userId, out _);
        }

        // Act
        var isRateLimited = _rateLimiter.ShouldRateLimit(userId, out var timeToWait);

        // Assert
        Assert.False(isRateLimited);
        Assert.Equal(TimeSpan.Zero, timeToWait);
    }
    
    [Fact]
    public void ShouldRateLimit_MultithreadingAccess_OneUser()
    {
        var requestsLimit = 1000;
        long userId = 1;
        var rateLimiter = new RateLimiterService(new RateLimitInfo(requestsLimit, TimeSpan.FromSeconds(5)));
        Parallel.For(0, requestsLimit - 1, _ =>
        {
            rateLimiter.ShouldRateLimit(userId, out var _);
        });

        // Последний 1000-й запрос, который должен быть пропущен
        var isLastRequestLimited = rateLimiter.ShouldRateLimit(userId, out _);
        Assert.False(isLastRequestLimited);
        
        // А вот 1001-й запрос уже не должен
        var isExceededRequestLimited = rateLimiter.ShouldRateLimit(userId, out _);
        Assert.True(isExceededRequestLimited);
    }
    
    [Fact]
    public void ShouldRateLimit_MultithreadingAccess_ManyUsers()
    {
        var usersCount = 1000;
        var requestsLimit = 2;
        var rateLimiter = new RateLimiterService(new RateLimitInfo(requestsLimit, TimeSpan.FromSeconds(5)));
        var usersIds = Enumerable.Range(1, usersCount).ToArray();
        // Имитируем первый запрос одновременно от 1000 пользователей
        usersIds.AsParallel()
            .ForAll(userId => rateLimiter.ShouldRateLimit(userId, out _));
        
        // Имитируем второй запрос одновременно от 1000 пользователей - он еще проходит
        usersIds.AsParallel()
            .ForAll(userId =>
            {
                var isLastRequestLimited = rateLimiter.ShouldRateLimit(userId, out _);
                Assert.False(isLastRequestLimited);
            });
        
        // Имитируем третий запрос одновременно от 1000 пользователей - он заблокируется
        usersIds.AsParallel()
            .ForAll(userId =>
            {
                var isExceededRequestLimited = rateLimiter.ShouldRateLimit(userId, out _);
                Assert.True(isExceededRequestLimited);
            });
    }
}
