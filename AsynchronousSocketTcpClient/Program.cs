// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Net.Sockets;

using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
await socket.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 8080));

Console.WriteLine("Type into the console to echo the contents");

var ns = new NetworkStream(socket);
var readTask = Console.OpenStandardInput().CopyToAsync(ns);
var writeTask = ns.CopyToAsync(Console.OpenStandardOutput());

// Quit if any of the tasks complete
await Task.WhenAny(readTask, writeTask);