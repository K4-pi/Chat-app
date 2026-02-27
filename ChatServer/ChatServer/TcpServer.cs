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

                    string[] result = msg.Split(':');
                    string response;

                    switch (result[0])
                    {
                        case "MSG":
                            Console.WriteLine("Storing message...");

                            //{roomID}@{text}"
                            string[] formated = result[1].Split('@', 2);
                            string roomID = formated[0];
                            string msgText = formated[1];

                            await database.StoreMessageAsync(roomID, msgText, client);

                            Console.WriteLine($"Broadcasting message to room {roomID}");
                            string senderId = ConnectionManager.GetUserId(client);
                            _ = database.BroadcastToRoomAsync(roomID, msgText, senderId);
                            break;

                        case "LOGIN":
                            Console.WriteLine("Authentication...");
                            response = database.Authenticate(result[1], client);
                            await ConnectionManager.SendAsync(response, client);
                            break;

                        case "GET_ROOMS":
                            Console.WriteLine("Sending list of rooms...");
                            string uid = ConnectionManager.GetUserId(client);
                            if (uid != null)
                            {
                                response = await database.GetUserRoomsAsync(uid);
                                await ConnectionManager.SendAsync(response, client);
                            }
                            break;

                        case "GET_HISTORY":
                            Console.WriteLine("Sending history of messages...");
                            string roomId = result[1];
                            await database.SendMessageHistoryAsync(roomId, client);
                            break;

                        default:
                            Console.WriteLine("No such operation " + result[0]);
                            Console.WriteLine(msg);
                            break;
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
