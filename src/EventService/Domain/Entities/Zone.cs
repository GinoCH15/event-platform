namespace EventService.Domain.Entities;

public class Zone
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public string Name { get; private set; } = default!;
    public decimal Price { get; private set; }
    public int Capacity { get; private set; }
    public int AvailableCapacity { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Zone() { }

    public static Zone Create(Guid eventId, string name, decimal price, int capacity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (price < 0)
            throw new DomainException("El precio no puede ser negativo.");
        if (capacity <= 0)
            throw new DomainException("La capacidad debe ser mayor a cero.");

        return new Zone
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Name = name.Trim(),
            Price = price,
            Capacity = capacity,
            AvailableCapacity = capacity,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void DecreaseCapacity(int quantity = 1)
    {
        if (AvailableCapacity < quantity)
            throw new DomainException($"Capacidad insuficiente en la zona '{Name}'.");
        AvailableCapacity -= quantity;
    }

    public void IncreaseCapacity(int quantity = 1)
    {
        if (AvailableCapacity + quantity > Capacity)
            throw new DomainException("No se puede exceder la capacidad total de la zona.");
        AvailableCapacity += quantity;
    }
}
