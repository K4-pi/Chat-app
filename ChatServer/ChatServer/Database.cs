using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

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

        private IMongoCollection<BsonDocument> GetUsersCollection(IMongoDatabase db)
        {
            return db.GetCollection<BsonDocument>("users");
        }

        public String Authenticate(string data)
        {
            var credentials = data.Split('@');
            string username = credentials[0];
            string password = credentials[1];

            Console.WriteLine($"Username: {username}");
            Console.WriteLine($"Password: {password}");

            var usersCollection = GetUsersCollection(database);

            var filter = Builders<BsonDocument>.Filter.Eq("Username", username);
            var userDoc = usersCollection.Find(filter).FirstOrDefault();

            if (userDoc != null && userDoc["Password"] == password)
            {
                string sessionId = Guid.NewGuid().ToString();
                //ActiveSessions.Add(sessionId, username);
                return $"AUTH_SUCCESS:{sessionId}";
            }

            return "AUTH_FAIL";
        }

    }
}
