using System.Net;
using System.Net.Sockets;
using System.Text;

using var listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, 8080));
//Loopback == localhost

Console.WriteLine($"Listening on {listenSocket.LocalEndPoint}");

listenSocket.Listen();

while (true)
{
    // Wait for a new connection to arrive
    using var connection = listenSocket.Accept();
    Console.WriteLine("Receive data...");
    var buffer = new byte[4096];
    int bytesReadCount;
    var memoryStream = new MemoryStream();

    bytesReadCount = connection.Receive(buffer);
    memoryStream.Write(buffer[..bytesReadCount]);
    
    byte[] bytes = memoryStream.ToArray();
    string message = Encoding.UTF8.GetString(bytes);
    Console.WriteLine($"Received message: {message}");
    switch (message)
    {
        case "StartWorking":
            StartWorking();
            connection.Send(Encoding.UTF8.GetBytes("OK")); //Опционально
            break;
        case "GetDayOfWeek":
            connection.Send(new[] { (byte) DateTime.Now.DayOfWeek });
            break;
        case "GetDateTime":
            long binaryTime = DateTime.Now.ToBinary();
            connection.Send(BitConverter.GetBytes(binaryTime));
            break;
        default:
            Console.WriteLine($"Unknown message: {message}");
            connection.Send(Encoding.UTF8.GetBytes("UNKNOWN MESSAGE"));
            break;
    }
}

void StartWorking()
{
}