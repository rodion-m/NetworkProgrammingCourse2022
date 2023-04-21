using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace SignalRMessenger.Server.Hubs;

/// <summary>
/// Пример хаба с поддержкой отправки сообщений конкретным пользователям.
/// Сначала пользователь должен зарегистрироваться, чтобы его ClientId можно было найти по имени.
/// После этого можно отправлять сообщения конкретному пользователю.
/// </summary>
public class ChatHubWithUserId : Hub
{
    private static readonly ConcurrentDictionary<string, string> UserConnections = new();

    public async Task SendMessage(string user, string message)
    {
        await Clients.Others.SendAsync("ReceiveMessage", user, message);
    }

    public async Task SendPrivateMessage(string targetUser, string user, string message)
    {
        if (UserConnections.TryGetValue(targetUser, out var targetConnectionId))
        {
            await Clients.Client(targetConnectionId).SendAsync("ReceiveMessage", user, message);
        }
    }

    public void RegisterUser(string username)
    {
        UserConnections[username] = Context.ConnectionId;
    }
}