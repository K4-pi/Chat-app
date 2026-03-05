using System.Net.Sockets;
using System.Text;

namespace ChatServer
{
    public static class ConnectionManager
    {
        private static Dictionary<string, TcpClient> _onlineUsers = new Dictionary<string, TcpClient>();

        public static Dictionary<string, TcpClient> GetOnlineUsers() 
        { 
            return _onlineUsers; 
        }

        public static void AddUser(string userId, TcpClient client)
        {
            _onlineUsers[userId] = client;
        }

        public static void RemoveUser(TcpClient client)
        {
            string uid = GetUserId(client);
            _onlineUsers.Remove(uid);
            Console.WriteLine($"\nRemoved user with ID: {uid} from table");
        }

        public static TcpClient GetClient(string userId)
        {
            _onlineUsers.TryGetValue(userId, out var client);
            return client;
        }

        public static string GetUserId(TcpClient client)
        {   
            var entry = _onlineUsers.FirstOrDefault(x => x.Value == client);
            return entry.Key;
        }
        public static async Task SendAsync(string data, TcpClient client)
        {
            byte[] res = Encoding.UTF8.GetBytes(data + "\n");
            await client.GetStream().WriteAsync(res, 0, res.Length);
        }
    }
}
