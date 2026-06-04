namespace NotificationService.Contracts;

/// <summary>
/// Contrato del mensaje EventCreated — debe coincidir exactamente
/// con el publicado por EventService.
/// </summary>
public record EventCreatedMessage
{
    public Guid MessageId { get; init; }
    public Guid EventId { get; init; }
    public string Name { get; init; } = default!;
    public DateTime Date { get; init; }
    public string Location { get; init; } = default!;
    public Guid OrganizerId { get; init; }
    public DateTime OccurredAt { get; init; }
    public Guid CorrelationId { get; init; }
    public int Version { get; init; }
}
