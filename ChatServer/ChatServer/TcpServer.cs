using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace ChatServer
{
    public static class TcpServer
    {
        public static async Task ClientHandler(TcpClient client)
        {
            var stream = client.GetStream();
            var buffer = new byte[4096];

            Database database = new Database();

            try
            {
                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer);
                    if (bytesRead == 0) break;

                    string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    Console.WriteLine($"\n{msg}");

                    string response;

                    if (msg.StartsWith("MSG:")) 
                    {
                        Console.WriteLine("Storing message...");
                        msg = msg.Substring(4);

                        //{roomID}@{text}"
                        string[] formated = msg.Split('@', 3);
                        string roomID = formated[0];
                        string msgText = formated[1];
                        string sendTime = formated[2];

                        await database.StoreMessageAsync(roomID, msgText, sendTime, client);

                        Console.WriteLine($"Broadcasting message to room {roomID}");
                        string senderId = ConnectionManager.GetUserId(client);
                        _ = database.BroadcastToRoomAsync(roomID, msgText, sendTime, senderId);
                    }
                    else if (msg.StartsWith("LOGIN:")) 
                    {
                        Console.WriteLine("Authentication...");
                        msg = msg.Substring(6);

                        response = database.Authenticate(msg, client);
                        await ConnectionManager.SendAsync(response, client);
                    }
                    else if (msg.StartsWith("GET_ROOMS"))
                    {
                        Console.WriteLine("Sending list of rooms...");

                        string uid = ConnectionManager.GetUserId(client);
                        if (uid != null)
                        {
                            response = await database.GetUserRoomsAsync(uid);
                            await ConnectionManager.SendAsync(response, client);
                        }
                    }
                    else if (msg.StartsWith("GET_HISTORY:")) 
                    {
                        Console.WriteLine("Sending history of messages...");
                        msg = msg.Substring(12);

                        string roomId = msg;
                        await database.SendMessageHistoryAsync(roomId, client);
                    }

                    Console.WriteLine("SWITCH END!");
                }
            }
            finally
            {
                client.Close();
                ConnectionManager.RemoveUser(client);
            }

        }

        
    }
}
