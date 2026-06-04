namespace EventService.Domain.Entities;

public class Event
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public DateTime Date { get; private set; }
    public string Location { get; private set; } = default!;
    public EventStatus Status { get; private set; }
    public Guid OrganizerId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<Zone> _zones = new();
    public IReadOnlyCollection<Zone> Zones => _zones.AsReadOnly();

    private Event() { }

    public static Event Create(string name, DateTime date, string location, Guid organizerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(location);

        if (date <= DateTime.UtcNow)
            throw new DomainException("La fecha del evento debe ser futura.");

        return new Event
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Date = date,
            Location = location.Trim(),
            OrganizerId = organizerId,
            Status = EventStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AddZone(Zone zone)
    {
        ArgumentNullException.ThrowIfNull(zone);
        if (_zones.Any(z => z.Name.Equals(zone.Name, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"Ya existe una zona con el nombre '{zone.Name}'.");
        _zones.Add(zone);
    }

    public void Publish()
    {
        if (Status != EventStatus.Draft)
            throw new DomainException("Solo se pueden publicar eventos en estado Draft.");
        if (!_zones.Any())
            throw new DomainException("El evento debe tener al menos una zona para publicarse.");
        Status = EventStatus.Published;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == EventStatus.Cancelled)
            throw new DomainException("El evento ya está cancelado.");
        Status = EventStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum EventStatus
{
    Draft = 0,
    Published = 1,
    Cancelled = 2,
    Finished = 3
}
