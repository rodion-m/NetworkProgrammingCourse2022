using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace SignalRMessenger.WPFClientMVVM.Services;

public class ChatService
{
    private readonly HubConnection _hubConnection;
    private readonly SynchronizationContext _synchronizationContext;
    
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
    
    public event Action? OnConnected;
    public event Action<ChatMessage>? OnMessageReceived;

    public ChatService(
        HubConnection hubConnection, 
        SynchronizationContext synchronizationContext)
    {
        _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));
        _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
    }

    public async ValueTask EnsureConnected()
    {
        if (IsConnected()) return;

        await _connectionSemaphore.WaitAsync();
        try
        {
            if (IsConnected()) return;

            _hubConnection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                _synchronizationContext.Post(_ =>
                {
                    OnMessageReceived?.Invoke(new ChatMessage(user, message));
                }, null);
            });

            await _hubConnection.StartAsync();
            OnConnected?.Invoke();
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }
    
    public async Task SendMessage(string userName, string message)
    {
        ArgumentNullException.ThrowIfNull(userName);
        ArgumentNullException.ThrowIfNull(message);
        await EnsureConnected();
        await _hubConnection.SendAsync("SendMessage", userName, message);
    }

    public bool IsConnected() => _hubConnection.State == HubConnectionState.Connected;
}