using ChatServer;
using System.Net;
using System.Net.Sockets;

IPAddress IpAddress = IPAddress.Any;
int Port = 9000;

var listener = new TcpListener(IpAddress, Port);
listener.Start();
Console.WriteLine($"Server listening on {IpAddress}:{Port}");

while (true) //MAIN LOOP
{
    var client = await listener.AcceptTcpClientAsync();
    _ = TcpServer.ClientHandler(client);
}