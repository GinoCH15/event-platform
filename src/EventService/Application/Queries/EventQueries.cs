using EventService.Application.DTOs;
using EventService.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventService.Application.Queries;

// ─── Queries ────────────────────────────────────────────────────────────────

public record GetEventsQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<EventSummaryDto>>;

public record GetEventByIdQuery(Guid Id) : IRequest<EventDto?>;

// ─── Handler: GetEvents ─────────────────────────────────────────────────────

public class GetEventsQueryHandler : IRequestHandler<GetEventsQuery, PagedResult<EventSummaryDto>>
{
    private readonly IEventRepository _eventRepo;
    private readonly IDistributedCache _cache;
    private readonly ILogger<GetEventsQueryHandler> _logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public GetEventsQueryHandler(
        IEventRepository eventRepo,
        IDistributedCache cache,
        ILogger<GetEventsQueryHandler> logger)
    {
        _eventRepo = eventRepo;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PagedResult<EventSummaryDto>> Handle(GetEventsQuery query, CancellationToken ct)
    {
        var cacheKey = $"events:list:p{query.Page}:ps{query.PageSize}";

        // 1. Intentar leer desde cache
        var cached = await _cache.GetStringAsync(cacheKey, ct);
        if (cached is not null)
        {
            _logger.LogDebug("Cache HIT: {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<PagedResult<EventSummaryDto>>(cached)!;
        }

        _logger.LogDebug("Cache MISS: {CacheKey}", cacheKey);

        // 2. Leer desde BD
        var events = await _eventRepo.GetAllAsync(query.Page, query.PageSize, ct);
        var total = await _eventRepo.CountAsync(ct);

        var items = events.Select(e => new EventSummaryDto(
            e.Id,
            e.Name,
            e.Date,
            e.Location,
            e.Status.ToString(),
            e.Zones.Sum(z => z.Capacity),
            e.Zones.Sum(z => z.AvailableCapacity)
        ));

        var result = new PagedResult<EventSummaryDto>(items, total, query.Page, query.PageSize);

        // 3. Guardar en cache
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl };
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), options, ct);

        return result;
    }
}

// ─── Handler: GetEventById ──────────────────────────────────────────────────

public class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, EventDto?>
{
    private readonly IEventRepository _eventRepo;
    private readonly IDistributedCache _cache;

    public GetEventByIdQueryHandler(IEventRepository eventRepo, IDistributedCache cache)
    {
        _eventRepo = eventRepo;
        _cache = cache;
    }

    public async Task<EventDto?> Handle(GetEventByIdQuery query, CancellationToken ct)
    {
        var cacheKey = $"events:detail:{query.Id}";

        var cached = await _cache.GetStringAsync(cacheKey, ct);
        if (cached is not null)
            return JsonSerializer.Deserialize<EventDto>(cached);

        var e = await _eventRepo.GetByIdAsync(query.Id, ct);
        if (e is null) return null;

        var dto = new EventDto(
            e.Id, e.Name, e.Date, e.Location, e.Status.ToString(), e.OrganizerId, e.CreatedAt,
            e.Zones.Select(z => new ZoneDto(z.Id, z.Name, z.Price, z.Capacity, z.AvailableCapacity)).ToList()
        );

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto), options, ct);

        return dto;
    }
}
