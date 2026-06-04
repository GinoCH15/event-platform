using MongoDB.Driver;

namespace NotificationService.Infrastructure.Persistence;

public class NotificationMongoContext
{
    private readonly IMongoDatabase _db;

    public NotificationMongoContext(IConfiguration config)
    {
        var connStr = config.GetConnectionString("MongoDB")
                      ?? "mongodb://localhost:27017";
        var dbName = config["MongoDB:Database"] ?? "notificationdb";

        var client = new MongoClient(connStr);
        _db = client.GetDatabase(dbName);

        EnsureIndexes();
    }

    public IMongoCollection<ProcessedMessage> ProcessedMessages
        => _db.GetCollection<ProcessedMessage>("processed_messages");

    public IMongoCollection<NotificationRecord> Notifications
        => _db.GetCollection<NotificationRecord>("notifications");

    private void EnsureIndexes()
    {
        // TTL index: elimina registros de mensajes procesados después de 30 días
        var ttlKey = Builders<ProcessedMessage>.IndexKeys.Ascending(x => x.ProcessedAt);
        var ttlOpts = new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(30) };
        ProcessedMessages.Indexes.CreateOne(new CreateIndexModel<ProcessedMessage>(ttlKey, ttlOpts));
    }
}
