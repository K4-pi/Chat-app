using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Chat.Utilities
{
    public class TcpChatClient
    {
        private TcpClient? client;
        private NetworkStream? stream;

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

        public async Task CloseConnectionAsync()
        {
            try
            {
                stream?.Close();
                client?.Close();
                client = null; 
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }

        // Listen after login
        public async Task ListenAsync(Action<string> onMessageReceived)
        {
            Debug.WriteLine("Started listen loop");
            try
            {
                if (stream == null || client == null) return;

                while (client.Connected)
                {
                    var ms = new MemoryStream(); // Needs to be here to 'clear' it
                    byte[] buffer = new byte[2048];
                    int bytesRead = 0;

                    do
                    {
                        bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0) return;
                        ms.Write(buffer, 0, bytesRead);
                    }
                    while (stream.DataAvailable);

                    string receivedData = Encoding.UTF8.GetString(ms.ToArray()).Trim();
                    onMessageReceived?.Invoke(receivedData); // Starts 'action' with received message
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Disconnected: {ex.Message}");
            }
            Debug.WriteLine("Ended listen loop");
        }

        public async Task SendAsync(string message)
        {
            if (stream == null) return; // "ERROR:Not Connected"

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message + '\n');
                await stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex) 
            { 
                Debug.WriteLine($"SendAsyncException:{ex.Message}"); 
            }
        }

        public async Task<string> SendAndReceiveAsync(string message)
        {
            try
            {
                if (stream == null) return "ERROR:Not Connected";

                // Send
                byte[] data = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);

                // Receive - use of MemoryStream() prevents data loss problem
                var ms = new MemoryStream();
                byte[] buffer = new byte[2048];

                do
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;
                    ms.Write(buffer, 0, bytesRead);
                }
                while (stream.DataAvailable);

                return Encoding.UTF8.GetString(ms.ToArray()).Trim();
            }
            catch (Exception ex) 
            { 
                Debug.WriteLine($"SendAndReceiveAsyncException{ex.Message}"); 
            }
            return "ERROR:catch";
        }
    }
}