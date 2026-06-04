using EventService.Domain.Entities;
using EventService.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EventService.Infrastructure.Persistence;

public class EventDbContext : DbContext, IUnitOfWork
{
    public EventDbContext(DbContextOptions<EventDbContext> options) : base(options) { }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<Zone> Zones => Set<Zone>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public async Task<int> CommitAsync(CancellationToken ct = default)
        => await SaveChangesAsync(ct);
}
