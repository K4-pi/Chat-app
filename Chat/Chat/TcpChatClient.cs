using System;
using System.Diagnostics;
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

        // Listen after login
        public async Task ListenAsync(Action<string> onMessageReceived)
        {
            Debug.WriteLine("Started listen loop");
            byte[] buffer = new byte[2048];
            try
            {
                while (client.Connected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // Server shut down the connection

                    string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    onMessageReceived?.Invoke(receivedData); // Sends message back via a callback
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Disconnected: {ex.Message}");
            }
            Debug.WriteLine("Ended listen loop");
        }

        public async void SendAsync(string message)
        {
            if (stream == null) return; // "ERROR:Not Connected"

            byte[] data = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(data, 0, data.Length);
        }

        public async Task<string> SendAndReceiveAsync(string message)
        {
            if (stream == null) return "ERROR:Not Connected";

            // Send
            byte[] data = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(data, 0, data.Length);

            // Receive
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
        }
    }
}