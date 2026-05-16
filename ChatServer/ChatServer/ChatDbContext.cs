using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;

namespace ChatServer
{
    public class ChatDbContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Room> Rooms { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;

        public static ChatDbContext Create(IMongoDatabase database) =>
            new(new DbContextOptionsBuilder<ChatDbContext>()
                .UseMongoDB(database.Client, database.DatabaseNamespace.DatabaseName)
                .Options);

        public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>().ToCollection("users");
            modelBuilder.Entity<Room>().ToCollection("rooms");
            modelBuilder.Entity<Message>().ToCollection("messages");
        }
    }
}
