using ChatServer;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Net;
using System.Net.Sockets;
using System.Text;

var listener = new TcpListener(IPAddress.Any, 9000);
listener.Start();
Console.WriteLine("Server started");

while (true) //MAIN LOOP
{
    var client = await listener.AcceptTcpClientAsync();
    _ = TcpServer.ClientHandler(client);
}