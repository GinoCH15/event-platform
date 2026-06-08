using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NotificationService.Infrastructure.Persistence;

/// <summary>
/// Registro de mensajes procesados para garantizar idempotencia.
/// Si el messageId ya existe en esta colección, el mensaje se ignora.
/// </summary>
public class ProcessedMessage
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid MessageId { get; set; }

    public string MessageType { get; set; } = default!;
    public DateTime ProcessedAt { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Registro de notificaciones enviadas.
/// </summary>
public class NotificationRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [BsonRepresentation(BsonType.String)]
    public Guid EventId { get; set; }
    [BsonRepresentation(BsonType.String)]
    public Guid MessageId { get; set; }
    public string Type { get; set; } = default!;   // Email, SMS, Push
    public string Recipient { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public string Body { get; set; } = default!;
    public string Status { get; set; } = "Sent";
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
