using System;
using System.Windows;
using System.Windows.Controls;
using Notification.Wpf;
using Wpf.Ui.Controls.Interfaces;
using Wpf.Ui.Mvvm.Contracts;
using SignalRMessenger.WPFClientMVVM.Services;
using SignalRMessenger.WPFClientMVVM.ViewModels;

namespace SignalRMessenger.WPFClientMVVM.Views.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INavigationWindow
    {
        private readonly ISnackbarService _snackbarService;
        private readonly ChatService _chatService;
        private readonly NotificationManager _notificationManager;

        public MainWindowViewModel ViewModel
        {
            get;
        }

        public MainWindow(
            MainWindowViewModel viewModel,
            IPageService pageService,
            INavigationService navigationService,
            ChatService chatService,
            NotificationManager notificationManager)
        {
            _chatService = chatService;
            _notificationManager = notificationManager;
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
            SetPageService(pageService);

            navigationService.SetNavigationControl(RootNavigation);
        }

        #region INavigationWindow methods

        public Frame GetFrame()
            => RootFrame;

        public INavigation GetNavigation()
            => RootNavigation;

        public bool Navigate(Type pageType)
            => RootNavigation.Navigate(pageType);

        public void SetPageService(IPageService pageService)
            => RootNavigation.PageService = pageService;

        public void ShowWindow()
            => Show();

        public void CloseWindow()
            => Close();

        #endregion INavigationWindow methods

        /// <summary>
        /// Raises the closed event.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Make sure that closing this window will begin the process of closing the application.
            Application.Current.Shutdown();
        }

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            await _chatService.EnsureConnected();
            _notificationManager.Show("Готов к общению!", NotificationType.Information);
            _chatService.OnMessageReceived += OnNewMessageReceived;
        }

        private void OnNewMessageReceived(ChatMessage message)
        {
            _notificationManager.Show(
                $"Новое сообщение от {message.User}", message.Content, NotificationType.Information);
        }
    }
}