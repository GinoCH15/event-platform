using EventService.Application.DTOs;
using EventService.Application.Events;
using EventService.Domain.Entities;
using EventService.Domain.Repositories;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventService.Application.Commands;

// ─── Command ───────────────────────────────────────────────────────────────

public record CreateEventCommand(
    string Name,
    DateTime Date,
    string Location,
    Guid OrganizerId,
    List<CreateZoneDto> Zones
) : IRequest<EventDto>;

// ─── Validator ──────────────────────────────────────────────────────────────

public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres.");

        RuleFor(x => x.Date)
            .GreaterThan(DateTime.UtcNow).WithMessage("La fecha debe ser futura.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("La ubicación es requerida.")
            .MaximumLength(500);

        RuleFor(x => x.OrganizerId)
            .NotEmpty().WithMessage("El organizador es requerido.");

        RuleFor(x => x.Zones)
            .NotEmpty().WithMessage("Se debe definir al menos una zona.")
            .Must(z => z.Count <= 20).WithMessage("No se pueden definir más de 20 zonas.");

        RuleForEach(x => x.Zones).ChildRules(zone =>
        {
            zone.RuleFor(z => z.Name).NotEmpty().MaximumLength(100);
            zone.RuleFor(z => z.Price).GreaterThanOrEqualTo(0);
            zone.RuleFor(z => z.Capacity).GreaterThan(0);
        });
    }
}

// ─── Handler ────────────────────────────────────────────────────────────────

public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, EventDto>
{
    private readonly IEventRepository _eventRepo;
    private readonly IUnitOfWork _uow;
    private readonly IPublishEndpoint _publisher;
    private readonly ILogger<CreateEventCommandHandler> _logger;

    public CreateEventCommandHandler(
        IEventRepository eventRepo,
        IUnitOfWork uow,
        IPublishEndpoint publisher,
        ILogger<CreateEventCommandHandler> logger)
    {
        _eventRepo = eventRepo;
        _uow = uow;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<EventDto> Handle(CreateEventCommand cmd, CancellationToken ct)
    {
        // 1. Crear entidad de dominio
        var @event = Domain.Entities.Event.Create(cmd.Name, cmd.Date, cmd.Location, cmd.OrganizerId);

        foreach (var zoneDto in cmd.Zones)
        {
            var zone = Zone.Create(@event.Id, zoneDto.Name, zoneDto.Price, zoneDto.Capacity);
            @event.AddZone(zone);
        }

        // 2. Persistir en BD (transacción)
        await _eventRepo.AddAsync(@event, ct);
        await _uow.CommitAsync(ct);

        _logger.LogInformation("Evento creado: {EventId} - {Name}", @event.Id, @event.Name);

        // 3. Publicar mensaje al broker (asíncrono)
        var message = new EventCreatedMessage
        {
            EventId = @event.Id,
            Name = @event.Name,
            Date = @event.Date,
            Location = @event.Location,
            OrganizerId = @event.OrganizerId
        };

        await _publisher.Publish(message, ct);

        _logger.LogInformation("Mensaje EventCreated publicado: {MessageId}", message.MessageId);

        // 4. Mapear y retornar DTO
        return MapToDto(@event);
    }

    private static EventDto MapToDto(Domain.Entities.Event e) => new(
        e.Id,
        e.Name,
        e.Date,
        e.Location,
        e.Status.ToString(),
        e.OrganizerId,
        e.CreatedAt,
        e.Zones.Select(z => new ZoneDto(z.Id, z.Name, z.Price, z.Capacity, z.AvailableCapacity)).ToList()
    );
}
