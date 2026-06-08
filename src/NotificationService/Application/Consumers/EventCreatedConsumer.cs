using MassTransit;
using MongoDB.Driver;
using EventService.Application.Events;
using NotificationService.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace NotificationService.Application.Consumers;

public class EventCreatedConsumer : IConsumer<EventCreatedMessage>
{
    private readonly NotificationMongoContext _mongo;
    private readonly ILogger<EventCreatedConsumer> _logger;

    public EventCreatedConsumer(
        NotificationMongoContext mongo,
        ILogger<EventCreatedConsumer> logger)
    {
        _mongo = mongo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EventCreatedMessage> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Mensaje recibido: {MessageId} | EventId: {EventId} | EventName: {Name}",
            message.MessageId, message.EventId, message.Name);

        // ─── Idempotencia: verificar si ya se procesó ─────────────────────────
        var filter = Builders<ProcessedMessage>.Filter.Eq(x => x.MessageId, message.MessageId);
        var existing = await _mongo.ProcessedMessages.Find(filter).FirstOrDefaultAsync();

        if (existing is not null)
        {
            _logger.LogWarning(
                "Mensaje duplicado ignorado: {MessageId} (procesado en {ProcessedAt})",
                message.MessageId, existing.ProcessedAt);
            return;
        }

        // ─── Registrar como procesado (upsert atómico) ─────────────────────────
        var processedRecord = new ProcessedMessage
        {
            MessageId = message.MessageId,
            MessageType = nameof(EventCreatedMessage),
            ProcessedAt = DateTime.UtcNow,
            Notes = $"Evento: {message.Name}"
        };

        await _mongo.ProcessedMessages.InsertOneAsync(processedRecord);

        // ─── Simular envío de notificación ─────────────────────────────────────
        // En producción: usar MailKit, Twilio, Firebase, etc.
        var notification = await SendEventCreatedNotificationAsync(message);
        await _mongo.Notifications.InsertOneAsync(notification);

        _logger.LogInformation(
            "Notificación enviada para evento {EventId}: '{Name}' programado el {Date:dd/MM/yyyy}",
            message.EventId, message.Name, message.Date);
    }

    private static Task<NotificationRecord> SendEventCreatedNotificationAsync(EventCreatedMessage msg)
    {
        // Aquí se integraría con MailKit, SendGrid, AWS SES, etc.
        var record = new NotificationRecord
        {
            EventId = msg.EventId,
            MessageId = msg.MessageId,
            Type = "Email",
            Recipient = $"organizer-{msg.OrganizerId}@eventplatform.com",
            Subject = $"✅ Evento '{msg.Name}' creado exitosamente",
            Body = $"""
                Hola,

                Tu evento ha sido registrado exitosamente:

                📌 Nombre: {msg.Name}
                📅 Fecha: {msg.Date:dd/MM/yyyy HH:mm} UTC
                📍 Lugar: {msg.Location}

                El sistema procesará la publicación del evento en breve.

                — Plataforma de Eventos
                """,
            Status = "Sent",
            SentAt = DateTime.UtcNow
        };

        return Task.FromResult(record);
    }
}
