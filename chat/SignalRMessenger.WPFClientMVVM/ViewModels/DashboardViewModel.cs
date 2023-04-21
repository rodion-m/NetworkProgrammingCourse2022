using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui.Common.Interfaces;
using SignalRMessenger.WPFClientMVVM.Services;

namespace SignalRMessenger.WPFClientMVVM.ViewModels;

public partial class DashboardViewModel : ObservableObject, INavigationAware
{
    private readonly ChatService _chatService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private string _currentMessage = string.Empty;
        
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private string _userName = string.Empty;
    
    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages = new();

    public DashboardViewModel(ChatService chatService)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
    }

    public void OnNavigatedTo()
    {
        _chatService.OnMessageReceived += Messages.Add;
    }

    public void OnNavigatedFrom()
    {
        _chatService.OnMessageReceived -= Messages.Add;
    }

    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task OnSendMessage()
    {
        Messages.Add(new ChatMessage(UserName, CurrentMessage));
        await _chatService.SendMessage(UserName, CurrentMessage);
        CurrentMessage = "";
    }
        
    private bool CanSendMessage()
    {
        return !string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrWhiteSpace(CurrentMessage);
    }
}