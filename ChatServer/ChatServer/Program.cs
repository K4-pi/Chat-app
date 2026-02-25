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

    try
    {
        while (true)
        {
            int bytesRead = await stream.ReadAsync(buffer);
            if (bytesRead == 0)
                break;

            string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine(msg);

            await stream.WriteAsync(buffer.AsMemory(0, bytesRead));
        }
    }
    finally
    {
        client.Close();
    }
}