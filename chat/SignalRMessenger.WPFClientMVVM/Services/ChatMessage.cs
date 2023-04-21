namespace SignalRMessenger.WPFClientMVVM.Services;

public record ChatMessage(string User, string Content)
{
    public override string ToString() => $"{User}: {Content}";
}