using ChatServer;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Net;
using System.Net.Sockets;
using System.Text;

var listener = new TcpListener(IPAddress.Any, 9000);
listener.Start();
Console.WriteLine("Server started");

while (true)
{
    var client = await listener.AcceptTcpClientAsync();
    _ = ClientHandler(client);
}

async Task ClientHandler(TcpClient client)
{
    var stream = client.GetStream();
    var buffer = new byte[4096];

    Database database = new Database();

    try
    {
        while (true)
        {
            int bytesRead = await stream.ReadAsync(buffer);
            if (bytesRead == 0)
                break;

            string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
            Console.WriteLine($"\n{msg}");

            string[] result = msg.Split(':');
            string response = "NO_SUCH_OPERATION";

            switch (result[0])
            {
                case "MSG":
                    Console.WriteLine(result[1]); break;
                case "LOGIN":
                    Console.WriteLine("Authentication...");
                    response = database.Authenticate(result[1]); 
                    break;
                default:
                    Console.WriteLine("No such operation " + result[0]);
                    Console.WriteLine(msg); 
                    break;                
            }

            byte[] responseBuffer = Encoding.UTF8.GetBytes(response);
            await stream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
            Console.WriteLine($"Sent: {response}");
        }
    }
    finally
    {
        client.Close();
    }
}