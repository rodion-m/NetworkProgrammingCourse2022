using SignalRMessenger.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

WebApplication app = builder.Build();
app.MapHub<ChatHub>("/chatHub");

app.MapGet("/", () => "Server is working.");

app.Run();
