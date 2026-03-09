using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace ChatServer
{
    internal class Database
    {
        private IMongoClient? client;
        private IMongoDatabase? database;

        public Database()
        {
            string connectionUri = "mongodb+srv://chat_db_user:VbAcD6aY3FPrywXd@chatapp.mpve4di.mongodb.net";

            try
            {
                client = new MongoClient(connectionUri);
                database = client.GetDatabase("ChatApp");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize MongoDB: {ex.Message}");
            }
        }

        /* =============================
         *          COLLECTIONS
         * =============================
         */

        private IMongoCollection<BsonDocument> GetUsersCollection()
        {
            return database!.GetCollection<BsonDocument>("users");
        }

        private IMongoCollection<BsonDocument> GetRoomsCollection()
        {
            return database!.GetCollection<BsonDocument>("rooms");
        }

        private IMongoCollection<BsonDocument> GetMessagesCollection()
        {
            return database!.GetCollection<BsonDocument>("messages");
        }

        /* =============================
         *          FUNCTIONS
         * =============================
         */

        public async Task SendMessageHistoryAsync(string roomId, TcpClient client)
        {
            var roomsCollection = GetRoomsCollection();
            var messagesCollection = GetMessagesCollection();

            var filter = Builders<BsonDocument>.Filter.AnyEq("RoomId", new ObjectId(roomId));

            var sort = Builders<BsonDocument>.Sort.Ascending("Timestamp");
            var messages = await messagesCollection.Find(filter)
                                                   .Sort(sort)
                                                   .Limit(50)
                                                   .ToListAsync();

            if (messages.Count == 0) return;

            foreach (var doc in messages)
            {
                string msgId = doc["_id"].AsObjectId.ToString();
                string senderId = doc["SenderId"].AsObjectId.ToString();
                string senderName = doc.Contains("SenderName") ? doc["SenderName"].AsString : "Unknown";
                string text = doc["Text"].AsString;
                DateTime timeStamp = doc["Timestamp"].ToUniversalTime();

                await ConnectionManager.SendAsync($"MSG:{senderName}@{text}@{timeStamp.ToString("HH:mm dd/MM/yyyy")}", client); // If change time here also change time in client app
            }
        }

        public async Task<string> GetUsersListAsync(string roomId)
        {
            Console.WriteLine($"Room ID:{roomId}");

            var roomsCollection = GetRoomsCollection();

            var roomFilter = Builders<BsonDocument>.Filter.AnyEq("_id", new ObjectId(roomId));
            var room = await roomsCollection.Find(roomFilter).FirstOrDefaultAsync();

            if (room == null) return "USERS_LIST:NONE";

            var members = room["Members"].AsBsonArray;

            var usersCollection = GetUsersCollection();

            var membersIds = members.Select(m => m.AsObjectId).ToList();
            var usersFilter = Builders<BsonDocument>.Filter.In("_id", membersIds);
            var users = await usersCollection.Find(usersFilter).ToListAsync();
            var names = users.Select(u => u["Username"].ToString());
            return "USERS_LIST:" + string.Join("@", names);
        }

        public async Task<string> GetUserRoomsAsync(string userId)
        {
            var roomsCollection = GetRoomsCollection();

            var filter = Builders<BsonDocument>.Filter.AnyEq("Members", new ObjectId(userId));
            var rooms = await roomsCollection.Find(filter).ToListAsync();

            if (rooms.Count == 0) return "ROOM_LIST:NONE";

            // Format: ID,Name@ID,Name
            var roomStrings = rooms.Select(r => $"{r["_id"]},{r["RoomName"]},{r["RoomCode"]}");
            return "ROOM_LIST:" + string.Join("@", roomStrings);
        }

        public async Task BroadcastToRoomAsync(string roomId, string messageText, string sendTime, string senderId)
        {
            Console.WriteLine("Broadcast function start...");
            var roomsCollection = GetRoomsCollection();
            var filter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(roomId));
            var room = await roomsCollection.Find(filter).FirstOrDefaultAsync();

            if (room == null || !room.Contains("Members")) return;

            var usersCollection = GetUsersCollection();
            var usersFilter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(senderId));
            var user = await usersCollection.Find(usersFilter).FirstOrDefaultAsync();

            string senderName = user.Contains("Username") ? user["Username"].AsString : "Unknown";

            var members = room["Members"].AsBsonArray;
            string protocolMessage = $"MSG:{senderName}@{messageText}@{DateTime.Parse(sendTime).ToString("HH:mm dd/MM/yyyy")}"; // Might change to fetching time from DB
            byte[] data = Encoding.UTF8.GetBytes(protocolMessage);

            foreach (var memberValue in members)
            {
                string memberId = memberValue.AsObjectId.ToString();
                Console.WriteLine($"Member ID in broadcast: {memberId}");

                TcpClient targetClient = ConnectionManager.GetClient(memberId); // Find connection

                if (targetClient != null && targetClient.Connected)
                {
                    try
                    {
                        await targetClient.GetStream().WriteAsync(data, 0, data.Length);
                        Console.WriteLine($"Sent to member: {memberId}");
                    }
                    catch {
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

            var roomsCollection = GetRoomsCollection();

            var roomFilter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(roomId));
            var room = await roomsCollection.Find(roomFilter).FirstOrDefaultAsync();

            if (room == null)
            {
                Console.WriteLine($"Error: Room with id '{roomId}' not found");
                return;
            }

            var usersCollection = GetUsersCollection();

            var userFilter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(senderId));
            var user = await usersCollection.Find(userFilter).FirstOrDefaultAsync();

            string senderName = "Unknown";

            if (user != null && user.Contains("Username")) senderName = user["Username"].AsString;

            Console.WriteLine($"RoomID: {roomId}");
            Console.WriteLine($"Sender ID: {senderId}");
            Console.WriteLine($"Message: {message}");

            var messageDoc = new BsonDocument
            {
                { "RoomId", room["_id"].AsObjectId },
                { "SenderId", new ObjectId(senderId) },
                { "SenderName", senderName },
                { "Text", message },
                { "Timestamp", DateTime.Parse(sendTime) }
            };

            var messagesCollection = GetMessagesCollection();
            await messagesCollection.InsertOneAsync(messageDoc);
        }

        /*
         * A little misleading beceause it evaluates if client is Owner or Member,
         * if Owner delete room from database
         * if member delete user from room 
         */
        public async Task<string> DeleteRoomAsync(string roomName, TcpClient client)
        {
            Console.WriteLine($"Room:{roomName}");
            string userId = ConnectionManager.GetUserId(client);

            if (userId == null)
            {
                Console.WriteLine("userid == null");
                return "DELETE_ROOM:FAILED";
            }

            var roomsCollection = GetRoomsCollection();
            var roomsFilter = Builders<BsonDocument>.Filter.Eq("RoomName", roomName);
            var room = await roomsCollection.Find(roomsFilter).FirstOrDefaultAsync();

            if (room == null)
            {
                Console.WriteLine("Room == null");
                return "DELETE_ROOM:FAILED";
            }

            var uid = new ObjectId(userId);

            if (room["Owner"] == uid)
            {
                var result = await roomsCollection.DeleteOneAsync(roomsFilter);

                if (result.DeletedCount > 0) return "DELETE_ROOM:DELETED";

                Console.WriteLine("Owner error");
                return "DELETE_ROOM:FAILED";
            }
            else
            {
                var update = Builders<BsonDocument>.Update.Pull("Members", uid);
                var result = await roomsCollection.UpdateOneAsync(roomsFilter, update);

                if (result.ModifiedCount > 0) return "DELETE_ROOM:LEFT";

                Console.WriteLine("Leave error");
                return "DELETE_ROOM:FAILED";
            }
        }

        public async Task<string> JoinRoomAsync(string roomName, string roomCode, TcpClient client)
        {
            string userID = ConnectionManager.GetUserId(client);
            if (userID == null) return "JOIN_ROOM:FAILED";

            var roomsCollection = GetRoomsCollection();
            var roomFilter = Builders<BsonDocument>.Filter.Eq("RoomName", roomName);
            var room = roomsCollection.Find(roomFilter).FirstOrDefault();

            if (room == null) return "JOIN_ROOM:NO_ROOM";

            var update = Builders<BsonDocument>.Update.AddToSet("Members", new ObjectId(userID));
            var result = await roomsCollection.UpdateOneAsync(roomFilter, update);

            if (result.ModifiedCount > 0) return "JOIN_ROOM:SUCCESS";
            
            return "JOIN_ROOM:FAILED";
        }

        public async Task<String> CreateRoomAsync(string roomName, string roomCode, TcpClient client)
        {
            string userID = ConnectionManager.GetUserId(client);
            if (userID == null) return "CREATE_ROOM:FAILED";

            var roomsCollection = GetRoomsCollection();
            var filter = Builders<BsonDocument>.Filter.Eq("RoomName", roomName);
            var room = roomsCollection.Find(filter).FirstOrDefault();

            if (room != null) return "CREATE_ROOM:EXISTS";

            var uid = new ObjectId(userID);
            var newRoom = new BsonDocument
            {
                { "Owner",  uid},
                { "RoomName", roomName },
                { "RoomCode", roomCode },
                { "Members", new BsonArray{ uid } 
                }
            };
            await roomsCollection.InsertOneAsync(newRoom);

            return "CREATE_ROOM:SUCCESS";
        }

        public async Task<String> RegisterUser(string data, TcpClient client)
        {
            string[] credentials = data.Split('@', 2);
            string username = credentials[0];
            string password = credentials[1];

            var usersCollection = GetUsersCollection();
            var usersFilter = Builders<BsonDocument>.Filter.Eq("Username", username);
            var userDoc = usersCollection.Find(usersFilter).FirstOrDefault();

            if (userDoc != null) return "REGISTER_FAIL";

            var userId = new ObjectId();
            userId = ObjectId.GenerateNewId();

            var newUser = new BsonDocument
            {
                { "_id", userId },
                { "Username", username },
                { "Password", BCrypt.Net.BCrypt.HashPassword(password) }
            };
            await usersCollection.InsertOneAsync(newUser);

            var roomsCollection = GetRoomsCollection();
            var roomsFilter = Builders<BsonDocument>.Filter.Eq("RoomName", "general");
            var update = Builders<BsonDocument>.Update.AddToSet("Members", userId);

            var result = await roomsCollection.UpdateOneAsync(roomsFilter, update);

            if (result.ModifiedCount > 0)
            {
                Console.WriteLine("User successfully added to room");
            }
            else
            {
                Console.WriteLine("User was already in the room");
            }

            return "REGISTER_SUCCESS";
        }

        public String Authenticate(string data, TcpClient client)
        {
            var credentials = data.Split('@', 2);
            string username = credentials[0];
            string password = credentials[1];

            Console.WriteLine($"Username: {username}");
            Console.WriteLine($"Password: {password}");

            var usersCollection = GetUsersCollection();

            var filter = Builders<BsonDocument>.Filter.Eq("Username", username);
            var userDoc = usersCollection.Find(filter).FirstOrDefault();

            if (userDoc != null && BCrypt.Net.BCrypt.Verify(password, userDoc["Password"]?.ToString()))
            {
                string userId = userDoc["_id"]?.ToString() ?? throw new Exception("User ID is missing in database!"); ;

                ConnectionManager.AddUser(userId, client);

                return $"AUTH_SUCCESS:{userId}";
            }

            return "AUTH_FAIL";
        }

    }
}
