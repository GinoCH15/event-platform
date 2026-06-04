using EventService.Domain.Entities;
using EventService.Domain.Repositories;
using EventService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventService.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly EventDbContext _ctx;

    public EventRepository(EventDbContext ctx) => _ctx = ctx;

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _ctx.Events
            .Include(e => e.Zones)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IEnumerable<Event>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
        => await _ctx.Events
            .Include(e => e.Zones)
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<int> CountAsync(CancellationToken ct = default)
        => await _ctx.Events.CountAsync(ct);

    public async Task AddAsync(Event @event, CancellationToken ct = default)
        => await _ctx.Events.AddAsync(@event, ct);

    public Task UpdateAsync(Event @event, CancellationToken ct = default)
    {
        _ctx.Events.Update(@event);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await _ctx.Events.AnyAsync(e => e.Id == id, ct);
}
