namespace ChatTOP;

public record RateLimitInfo(int RequestsCount, TimeSpan Interval);