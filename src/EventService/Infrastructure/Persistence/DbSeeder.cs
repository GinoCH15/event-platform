using EventService.Domain.Entities;
using EventService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventService.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(EventDbContext ctx)
    {
        await ctx.Database.MigrateAsync();

        if (await ctx.Events.AnyAsync()) return;

        var organizerId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var event1 = Event.Create(
            "Concierto Rock en Lima",
            DateTime.UtcNow.AddDays(30),
            "Estadio Nacional, Lima",
            organizerId
        );
        event1.AddZone(Zone.Create(event1.Id, "Campo", 50.00m, 5000));
        event1.AddZone(Zone.Create(event1.Id, "Tribuna", 80.00m, 3000));
        event1.AddZone(Zone.Create(event1.Id, "Palco VIP", 200.00m, 500));

        var event2 = Event.Create(
            "Festival de Jazz 2026",
            DateTime.UtcNow.AddDays(60),
            "Parque de la Exposición, Lima",
            organizerId
        );
        event2.AddZone(Zone.Create(event2.Id, "General", 35.00m, 2000));
        event2.AddZone(Zone.Create(event2.Id, "Premium", 120.00m, 300));

        await ctx.Events.AddRangeAsync(event1, event2);
        await ctx.SaveChangesAsync();

        Console.WriteLine("✅ Seed completado: 2 eventos de ejemplo creados.");
    }
}
