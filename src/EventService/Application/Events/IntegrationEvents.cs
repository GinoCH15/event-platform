namespace EventService.Application.Events;

/// <summary>
/// Mensaje publicado al broker cuando un evento es creado.
/// Contrato compartido entre microservicios.
/// </summary>
public record EventCreatedMessage
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public Guid EventId { get; init; }
    public string Name { get; init; } = default!;
    public DateTime Date { get; init; }
    public string Location { get; init; } = default!;
    public Guid OrganizerId { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
    public int Version { get; init; } = 1;
}

/// <summary>
/// Mensaje publicado cuando un evento es publicado (estado Draft → Published).
/// </summary>
public record EventPublishedMessage
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public Guid EventId { get; init; }
    public string Name { get; init; } = default!;
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
    public int Version { get; init; } = 1;
}
