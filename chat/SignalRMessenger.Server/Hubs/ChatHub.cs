using Microsoft.AspNetCore.SignalR;

namespace SignalRMessenger.Server.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            // В случае если кто-то отправил сообщение,
            // сервер отправляем всем остальным клиентам событие "ReceiveMessage"
            await Clients.Others.SendAsync("ReceiveMessage", user, message);
        }
    }


}
