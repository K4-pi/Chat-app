using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Chat
{
    public class TcpChatClient
    {
        private TcpClient client;
        private NetworkStream stream;

        public async Task<bool> ConnectAsync(string ip, int port)
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(ip, port);
                stream = client.GetStream();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> SendAndReceiveAsync(string message)
        {
            if (stream == null) return "ERROR:Not Connected";

            // Send
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");
            await stream.WriteAsync(data, 0, data.Length);

            // Receive
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
        }
    }
}