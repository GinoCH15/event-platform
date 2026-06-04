using EventService.Application.Commands;
using EventService.Application.DTOs;
using EventService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Lista todos los eventos publicados con paginación (con cache Redis).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<EventSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var result = await _mediator.Send(new GetEventsQuery(page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene el detalle de un evento por ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEventByIdQuery(id), ct);
        return result is null ? NotFound(new { message = "Evento no encontrado." }) : Ok(result);
    }

    /// <summary>
    /// Crea un nuevo evento con sus zonas. Solo para admins/organizadores.
    /// Publica mensaje EventCreated al broker de forma asíncrona.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin,organizer")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateEvent(
        [FromBody] CreateEventDto dto,
        CancellationToken ct)
    {
        // Extraer organizerId del JWT; si no existe, usar valor por defecto (demo)
        var organizerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                               ?? User.FindFirstValue("sub");

        var organizerId = Guid.TryParse(organizerIdClaim, out var parsed)
            ? parsed
            : Guid.Parse("00000000-0000-0000-0000-000000000001");

        var command = new CreateEventCommand(
            dto.Name,
            dto.Date,
            dto.Location,
            organizerId,
            dto.Zones
        );

        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetEventById), new { id = result.Id }, result);
    }
}
