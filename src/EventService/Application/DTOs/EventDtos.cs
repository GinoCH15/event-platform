using EventService.Domain.Entities;

namespace EventService.Application.DTOs;

public record CreateZoneDto(
    string Name,
    decimal Price,
    int Capacity
);

public record CreateEventDto(
    string Name,
    DateTime Date,
    string Location,
    List<CreateZoneDto> Zones
);

public record ZoneDto(
    Guid Id,
    string Name,
    decimal Price,
    int Capacity,
    int AvailableCapacity
);

public record EventDto(
    Guid Id,
    string Name,
    DateTime Date,
    string Location,
    string Status,
    Guid OrganizerId,
    DateTime CreatedAt,
    List<ZoneDto> Zones
);

public record EventSummaryDto(
    Guid Id,
    string Name,
    DateTime Date,
    string Location,
    string Status,
    int TotalCapacity,
    int AvailableCapacity
);

public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize
)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
