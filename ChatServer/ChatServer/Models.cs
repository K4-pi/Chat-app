using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ChatServer
{
    public class User
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class Room
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public ObjectId Owner { get; set; }
        public string RoomName { get; set; } = null!;
        public string RoomCode { get; set; } = null!;
        public List<ObjectId> Members { get; set; } = new();
    }

    public class Message
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public ObjectId RoomId { get; set; }
        public ObjectId SenderId { get; set; }
        public string SenderName { get; set; } = null!;
        public string Text { get; set; } = null!;
        public DateTime Timestamp { get; set; }
    }
}
