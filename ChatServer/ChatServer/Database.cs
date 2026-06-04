using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace ChatServer
{
    internal class Database
    {
        private readonly ChatDbContext _db;

        public Database()
        {
            const string connectionUri = "mongodb+srv://chat_db_user:VbAcD6aY3FPrywXd@mainclusters.mpve4di.mongodb.net";
            var client = new MongoClient(connectionUri);
            var mongoDb = client.GetDatabase("ChatApp");
            _db = ChatDbContext.Create(mongoDb);
        }

        /* =============================
         *          FUNCTIONS
         * =============================
         */

        public async Task ChangeUsernameAsync(string username, TcpClient client)
        {
            string uid = ConnectionManager.GetUserId(client);

            if (!ObjectId.TryParse(uid, out ObjectId objectId))
            {
                await ConnectionManager.SendAsync("UPDATE_USERNAME_FAIL", client);
                return;
            }

            var obj = await _db.Users
                .Where(u => u.Id == objectId)
                .FirstOrDefaultAsync();

            if (obj == null)
            {
                await ConnectionManager.SendAsync("UPDATE_USERNAME_FAIL", client);
                return;
            }

            obj.Username = username;

            await _db.SaveChangesAsync();

            await ConnectionManager.SendAsync("UPDATE_USERNAME_SUCCESS", client);
        }

        public async Task ChangePasswordAsync(string newPassword, TcpClient client)
        {
            string uid = ConnectionManager.GetUserId(client);

            if (!ObjectId.TryParse(uid, out ObjectId objectId))
            {
                await ConnectionManager.SendAsync("UPDATE_PASSWORD_FAIL", client);
                return;
            }

            var obj = await _db.Users
                .Where(u => u.Id == objectId)
                .FirstOrDefaultAsync();

            if (obj == null)
            {
                await ConnectionManager.SendAsync("UPDATE_PASSWORD_FAIL", client);
                return;
            }

            obj.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);

            await _db.SaveChangesAsync();

            await ConnectionManager.SendAsync("UPDATE_PASSWORD_SUCCESS", client);
        }

        public async Task SendMessageHistoryAsync(string roomId, TcpClient client)
        {
            var oid = new ObjectId(roomId);

            var messages = await _db.Messages
                .Where(m => m.RoomId == oid)
                .OrderBy(m => m.Timestamp)
                .Take(50)
                .ToListAsync();

            if (messages.Count == 0) return;

            foreach (var msg in messages)
            {
                await ConnectionManager.SendAsync(
                    $"MSG:{roomId}@{msg.SenderName}@{msg.Text}@{msg.Timestamp.ToUniversalTime():HH:mm dd/MM/yyyy}",
                    client);
            }
        }

        public async Task<string> GetUsersListAsync(string roomId)
        {
            Console.WriteLine($"Room ID:{roomId}");

            var oid = new ObjectId(roomId);
            var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == oid);

            if (room == null) return "USERS_LIST:NONE";

            var users = await _db.Users
                .Where(u => room.Members.Contains(u.Id))
                .ToListAsync();

            var names = users.Select(u => u.Username);
            return "USERS_LIST:" + string.Join("@", names);
        }

        public async Task<string> GetUserRoomsAsync(string userId)
        {
            var oid = new ObjectId(userId);

            var rooms = await _db.Rooms
                .Where(r => r.Members.Contains(oid))
                .ToListAsync();

            if (rooms.Count == 0) return "ROOM_LIST:NONE";

            var roomStrings = rooms.Select(r => $"{r.Id},{r.RoomName},{r.RoomCode}");
            return "ROOM_LIST:" + string.Join("@", roomStrings);
        }

        public async Task BroadcastToRoomAsync(string roomId, string messageText, string sendTime, string senderId)
        {
            Console.WriteLine("Broadcast function start...");

            var rid = new ObjectId(roomId);
            var uid = new ObjectId(senderId);

            var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == rid);
            if (room == null) return;

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == uid);
            string senderName = user?.Username ?? "Unknown";

            string protocolMessage = $"MSG:{roomId}@{senderName}@{messageText}@{DateTime.Parse(sendTime):HH:mm dd/MM/yyyy}";
            byte[] data = Encoding.UTF8.GetBytes(protocolMessage);

            foreach (var memberId in room.Members)
            {
                Console.WriteLine($"Member ID in broadcast: {memberId}");
                TcpClient? targetClient = ConnectionManager.GetClient(memberId.ToString());

                if (targetClient != null && targetClient.Connected)
                {
                    try
                    {
                        await targetClient.GetStream().WriteAsync(data);
                        Console.WriteLine($"Sent to member: {memberId}");
                    }
                    catch
                    {
                        Debug.WriteLine("Disconnected...");
                    }
                }
                else
                {
                    Console.WriteLine("This client is not connected");
                }
            }
        }

        public async Task StoreMessageAsync(string roomId, string message, string sendTime, TcpClient client)
        {
            string senderId = ConnectionManager.GetUserId(client);
            if (string.IsNullOrEmpty(senderId))
            {
                Debug.WriteLine("NULL sender");
                return;
            }

            var rid = new ObjectId(roomId);
            var uid = new ObjectId(senderId);

            var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == rid);
            if (room == null)
            {
                Console.WriteLine($"Error: Room with id '{roomId}' not found");
                return;
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == uid);
            string senderName = user?.Username ?? "Unknown";

            Console.WriteLine($"RoomID: {roomId}");
            Console.WriteLine($"Sender ID: {senderId}");
            Console.WriteLine($"Message: {message}");

            _db.Messages.Add(new Message
            {
                RoomId = rid,
                SenderId = uid,
                SenderName = senderName,
                Text = message,
                Timestamp = DateTime.Parse(sendTime)
            });

            await _db.SaveChangesAsync();
        }

        public async Task<string> DeleteRoomAsync(string roomName, TcpClient client)
        {
            Console.WriteLine($"Room:{roomName}");
            string userId = ConnectionManager.GetUserId(client);
            if (userId == null) return "DELETE_ROOM:FAILED";

            var room = await _db.Rooms.FirstOrDefaultAsync(r => r.RoomName == roomName);
            if (room == null) return "DELETE_ROOM:FAILED";

            var uid = new ObjectId(userId);

            if (room.Owner == uid)
            {
                _db.Rooms.Remove(room);
                await _db.SaveChangesAsync();
                return "DELETE_ROOM:DELETED";
            }
            else
            {
                room.Members.Remove(uid);
                await _db.SaveChangesAsync();
                return "DELETE_ROOM:LEFT";
            }
        }

        public async Task<string> JoinRoomAsync(string roomName, string roomCode, TcpClient client)
        {
            string userID = ConnectionManager.GetUserId(client);
            if (userID == null) return "JOIN_ROOM:FAILED";

            var room = await _db.Rooms.FirstOrDefaultAsync(r => r.RoomName == roomName);
            if (room == null) return "JOIN_ROOM:NO_ROOM";

            var uid = new ObjectId(userID);
            if (room.Members.Contains(uid)) return "JOIN_ROOM:FAILED"; // already a member

            room.Members.Add(uid);
            await _db.SaveChangesAsync();
            return "JOIN_ROOM:SUCCESS";
        }

        public async Task<string> CreateRoomAsync(string roomName, string roomCode, TcpClient client)
        {
            string userID = ConnectionManager.GetUserId(client);
            if (userID == null) return "CREATE_ROOM:FAILED";

            var existing = await _db.Rooms.FirstOrDefaultAsync(r => r.RoomName == roomName);
            if (existing != null) return "CREATE_ROOM:EXISTS";

            var uid = new ObjectId(userID);
            _db.Rooms.Add(new Room
            {
                Owner = uid,
                RoomName = roomName,
                RoomCode = roomCode,
                Members = new List<ObjectId> { uid }
            });

            await _db.SaveChangesAsync();
            return "CREATE_ROOM:SUCCESS";
        }

        public async Task<string> RegisterUser(string data, TcpClient client)
        {
            string[] credentials = data.Split('@', 2);
            string username = credentials[0];
            string password = credentials[1];

            var existing = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (existing != null) return "REGISTER_FAIL";

            var userId = ObjectId.GenerateNewId();
            _db.Users.Add(new User
            {
                Id = userId,
                Username = username,
                Password = BCrypt.Net.BCrypt.HashPassword(password)
            });
            await _db.SaveChangesAsync();

            var general = await _db.Rooms.FirstOrDefaultAsync(r => r.RoomName == "general");
            if (general != null && !general.Members.Contains(userId))
            {
                general.Members.Add(userId);
                await _db.SaveChangesAsync();
                Console.WriteLine("User successfully added to room");
            }
            else
            {
                Console.WriteLine("User was already in the room");
            }

            return "REGISTER_SUCCESS";
        }

        public string Authenticate(string data, TcpClient client)
        {
            string[] credentials = data.Split('@', 2);
            string username = credentials[0];
            string password = credentials[1];

            Console.WriteLine($"Username: {username}");
            Console.WriteLine($"Password: {password}");

            var user = _db.Users.FirstOrDefault(u => u.Username == username);

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                string userId = user.Id.ToString();
                ConnectionManager.AddUser(userId, client);
                return $"AUTH_SUCCESS:{userId}";
            }

            return "AUTH_FAIL";
        }
    }
}