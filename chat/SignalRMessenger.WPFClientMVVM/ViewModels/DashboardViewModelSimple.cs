using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Wpf.Ui.Common.Interfaces;

namespace SignalRMessenger.WPFClientMVVM.ViewModels;

public partial class DashboardViewModelSimple : ObservableObject, INavigationAware
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private string _currentMessage = string.Empty;
        
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private string _userName = string.Empty;
        
    private HubConnection _hubConnection;

    public ObservableCollection<string> Messages { get; } = new();

    public void OnNavigatedTo()
    {
    }

    public void OnNavigatedFrom()
    {
    }
        
    [RelayCommand]
    private async Task OnInitializeSignalR()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("https://localhost:7000/chatHub")
            .Build();
        var synchronizationContext = SynchronizationContext.Current!;
        _hubConnection.On<string, string>("ReceiveMessage", (user, message) =>
        {
            synchronizationContext.Post(_ =>
            {
                Messages.Add($"{user}: {message}");
            }, null);
        });

        await _hubConnection.StartAsync();
    }

    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task OnSendMessage()
    {
        await _hubConnection.SendAsync("SendMessage", UserName, CurrentMessage);
        CurrentMessage = "";
    }
        
    private bool CanSendMessage()
    {
        return !string.IsNullOrWhiteSpace(UserName) 
               && !string.IsNullOrWhiteSpace(CurrentMessage)
               && _hubConnection is { State: HubConnectionState.Connected };
    }
}