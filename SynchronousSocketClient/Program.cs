using System.Net;
using System.Net.Sockets;
using System.Text;

using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
socket.Connect(new IPEndPoint(IPAddress.Loopback, 8080));

Console.WriteLine("Type message to server");
string? message = Console.ReadLine();
var encoding = Encoding.UTF8;
byte[] bytes = encoding.GetBytes(message);
socket.Send(bytes);

//Чтение ответа:
var buffer = new byte[4096];
var readCount = socket.Receive(buffer);
byte[] responseBytes = buffer[..readCount];
if (message == "GetDayOfWeek")
{
    byte dayOfWeekAsByte = responseBytes[0];
    DayOfWeek dayOfWeek = (DayOfWeek) dayOfWeekAsByte;
    Console.WriteLine(dayOfWeek);
}
else
{
    var responseText = Encoding.UTF8.GetString(responseBytes);
    Console.WriteLine(responseText);
}