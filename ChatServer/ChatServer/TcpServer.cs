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
                    var ms = new MemoryStream();

                    do
                    {
                        int bytesRead = await stream.ReadAsync(buffer);
                        if (bytesRead == 0) // MIGHT DISCCONECT EVEN IF CLIENT HAVEN'T DISCCONECTED ????
                        {
                            Console.WriteLine("Client disconnected");
                            return;
                        }
                        ms.Write(buffer, 0, bytesRead);
                    }
                    while (stream.DataAvailable);

                    string msg = Encoding.UTF8.GetString(ms.ToArray()).Trim();
                    Console.WriteLine($"\n{msg}");

                    string response;

                    if (msg.StartsWith("MSG:")) 
                    {
                        Console.WriteLine("Storing message...");
                        msg = msg.Substring(4);

                        //{roomID}@{text}@{time}"
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

                        await database.SendMessageHistoryAsync(msg, client);
                    }
                    else if (msg.StartsWith("REGISTER:"))
                    {
                        Console.WriteLine("Registering user...");
                        msg = msg.Substring(9);

                        response = await database.RegisterUser(msg, client);
                        await ConnectionManager.SendAsync(response, client);
                    }
                    else if (msg.StartsWith("GET_USERS_LIST:")) //$"GET_USERS_LIST:{currentRoomId}"
                    {
                        Console.WriteLine("Users list request...");
                        msg = msg.Substring(15);

                        response = await database.GetUsersListAsync(msg);

                        await ConnectionManager.SendAsync(response, client);
                    }
                    else if (msg.StartsWith("CREATE_ROOM:")) // $"CREATE_ROOM:{roomName}@{roomCode}"
                    {
                        Console.WriteLine("Creating room...");
                        msg = msg.Substring(12);
                        string[] tokens = msg.Split('@', 2);

                        response = await database.CreateRoomAsync(tokens[0], tokens[1], client);

                        await ConnectionManager.SendAsync(response, client);
                    }
                    else if (msg.StartsWith("JOIN_ROOM:")) // $"JOIN_ROOM:{roomName}@{roomCode}"
                    {
                        Console.WriteLine("Joining room...");
                        msg = msg.Substring(10);
                        string[] tokens = msg.Split('@', 2);

                        response = await database.JoinRoomAsync(tokens[0], tokens[1], client);

                        await ConnectionManager.SendAsync(response, client);
                    }
                    else if (msg.StartsWith("DELETE_ROOM:"))
                    {
                        Console.WriteLine("Deleting room...");
                        msg = msg.Substring(12);

                        response = await database.DeleteRoomAsync(msg, client);

                        await ConnectionManager.SendAsync(response, client);
                    }
                    else if (msg.StartsWith("CHANGE_USERNAME:"))
                    {
                        Console.WriteLine("CHANGE_USERNAME");
                        msg = msg.Substring(16);

                        await database.ChangeUsernameAsync(msg, client);
                    }
                    else if (msg.StartsWith("CHANGE_PASSWORD:"))
                    {
                        Console.WriteLine("CHANGE_PASSWORD");
                        msg = msg.Substring(16);

                        await database.ChangePasswordAsync(msg, client);
                    }
                    else if (msg.StartsWith("DISCONNECT"))
                    {
                        break; // Logouts user
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
