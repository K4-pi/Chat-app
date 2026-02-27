using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Servers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace ChatServer
{
    internal class Database
    {
        private IMongoClient client;
        private IMongoDatabase database;

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
            return database.GetCollection<BsonDocument>("users");
        }

        private IMongoCollection<BsonDocument> GetRoomsCollection()
        {
            return database.GetCollection<BsonDocument>("rooms");
        }

        private IMongoCollection<BsonDocument> GetMessagesCollection()
        {
            return database.GetCollection<BsonDocument>("messages");
        }

        /* =============================
         *          FUNCTIONS
         * =============================
         */

        public async Task<List<BsonDocument>> GetUserRooms(string userId)
        {
            var roomsCollection = GetRoomsCollection();

            var filter = Builders<BsonDocument>.Filter.AnyEq("Members", userId);

            var rooms = await roomsCollection.Find(filter).ToListAsync();
            return rooms;
        }

        public async Task BroadcastToRoomAsync(string roomId, string messageText, string senderId)
        {
            Console.WriteLine("Broadcast function start...");
            var roomsCollection = GetRoomsCollection();
            var filter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(roomId));
            var room = await roomsCollection.Find(filter).FirstOrDefaultAsync();

            if (room == null || !room.Contains("Members")) return;

            var members = room["Members"].AsBsonArray;
            string protocolMessage = $"MSG:{senderId}@{messageText}\n"; // Add \n for protocol separation
            byte[] data = Encoding.UTF8.GetBytes(protocolMessage);

            foreach (var memberValue in members)
            {
                string memberId = memberValue.AsObjectId.ToString();
                Console.WriteLine($"Member ID in broadcast: {memberId}");

                // FIND the connection for THIS specific member
                TcpClient targetClient = ConnectionManager.GetClient(memberId);

                if (targetClient != null && targetClient.Connected)
                {
                    try
                    {
                        // Write to the TARGET, not the original sender
                        await targetClient.GetStream().WriteAsync(data, 0, data.Length);
                        Console.WriteLine($"Sent to member: {memberId}");
                    }
                    catch { /* Handle disconnects */ }
                }
                else
                {
                    Console.WriteLine("This client is not connected");
                }
            }
        }

        public async Task StoreMessageAsync(string roomId, string message, TcpClient client)
        {
            string senderId = ConnectionManager.GetUserId(client);

            if (string.IsNullOrEmpty(senderId))
            {
                Debug.WriteLine("NULL sender");
                return;
            }

            var roomsCollection = GetRoomsCollection();
            var messagesCollection = GetMessagesCollection();

            var roomFilter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(roomId));
            var room = await roomsCollection.Find(roomFilter).FirstOrDefaultAsync();

            if (room == null)
            {
                Console.WriteLine($"Error: Room '{roomId}' not found.");
                return;
            }

            Console.WriteLine($"RoomID: {roomId}");
            Console.WriteLine($"Sender ID: {senderId}");
            Console.WriteLine($"Message: {message}");

            var messageDoc = new BsonDocument
            {
                { "RoomId", room["_id"].AsObjectId },
                { "SenderId", new ObjectId(senderId) },
                //{ "SenderName", sender },
                { "Text", message },
                { "Timestamp", DateTime.UtcNow }
            };

            await messagesCollection.InsertOneAsync(messageDoc);
        }

        public String Authenticate(string data, TcpClient client)
        {
            var credentials = data.Split('@');
            string username = credentials[0];
            string password = credentials[1];

            Console.WriteLine($"Username: {username}");
            Console.WriteLine($"Password: {password}");

            var usersCollection = GetUsersCollection();

            var filter = Builders<BsonDocument>.Filter.Eq("Username", username);
            var userDoc = usersCollection.Find(filter).FirstOrDefault();

            if (userDoc != null && userDoc["Password"] == password)
            {
                string userId = userDoc["_id"].ToString();

                ConnectionManager.AddUser(userId, client);

                return $"AUTH_SUCCESS:{userId}";
            }

            return "AUTH_FAIL";
        }

    }
}
