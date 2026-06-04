using EventService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventService.Infrastructure.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Date)
            .HasColumnName("date")
            .IsRequired();

        builder.Property(e => e.Location)
            .HasColumnName("location")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.OrganizerId)
            .HasColumnName("organizer_id")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasMany(e => e.Zones)
            .WithOne()
            .HasForeignKey(z => z.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.Status).HasDatabaseName("ix_events_status");
        builder.HasIndex(e => e.Date).HasDatabaseName("ix_events_date");
        builder.HasIndex(e => e.OrganizerId).HasDatabaseName("ix_events_organizer_id");
    }
}

public class ZoneConfiguration : IEntityTypeConfiguration<Zone>
{
    public void Configure(EntityTypeBuilder<Zone> builder)
    {
        builder.ToTable("zones");

        builder.HasKey(z => z.Id);
        builder.Property(z => z.Id).HasColumnName("id");

        builder.Property(z => z.EventId).HasColumnName("event_id").IsRequired();

        builder.Property(z => z.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(z => z.Price)
            .HasColumnName("price")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(z => z.Capacity)
            .HasColumnName("capacity")
            .IsRequired();

        builder.Property(z => z.AvailableCapacity)
            .HasColumnName("available_capacity")
            .IsRequired();

        builder.Property(z => z.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(z => z.EventId).HasDatabaseName("ix_zones_event_id");
    }
}
