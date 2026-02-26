using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace ChatServer
{
    public static class ConnectionManager
    {
        private static Dictionary<string, TcpClient> _onlineUsers = new Dictionary<string, TcpClient>();

        public static void AddUser(string userId, TcpClient client)
        {
            _onlineUsers[userId] = client;
        }

        public static void RemoveUser(string userId)
        {
            _onlineUsers.Remove(userId);
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

    }
}
