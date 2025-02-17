
using DriverService.Core.Dtos;
using DriverService.Core.Features.Driver.Commands;
using DriverService.Core.Features.Driver.Queries;
using DriverService.Domain.Entities;
using DriverService.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DriversController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMongoDbService _mongoDbService;
    private readonly ILogger<DriversController> _logger;
    private readonly RedisCacheService _redisCacheService;
    public DriversController(IMediator mediator, IMongoDbService mongoDbService, ILogger<DriversController> logger, RedisCacheService redisCacheService)
    {
        _mediator = mediator;
        _mongoDbService = mongoDbService;
        _logger = logger;
        _redisCacheService = redisCacheService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDriver(Guid id)
    {
        var result = await _mediator.Send(new GetDriverByIdQuery(id));
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDriver(Guid id)
    {
        var result = await _mediator.Send(new DeleteDriverCommand(id));
        return Ok(result ? "driver deleted successfully" : "driver not found");
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableDrivers()
    {
        var result = await _mediator.Send(new GetAvailableDriversQuery());
        return Ok(result);
    }

    [HttpPost("{id}/location")]
    public async Task<IActionResult> UpdateLocation(
        Guid id,
        [FromBody] LocationUpdateDto dto)
    {
        await _mediator.Send(new UpdateDriverLocationCommand(id, dto.Latitude, dto.Longitude));
        return Accepted();
    }
    [HttpGet("{id}/location")]
    public async Task<IActionResult> GetDriverLocation(Guid id)
    {
        // Check cache first
        var cachedLocation = await _redisCacheService.GetCachedLocationAsync(id);
        if (cachedLocation.HasValue)
        {
            return Ok(new DriverLocationResponse(
                id,
                cachedLocation.Value.lat,
                cachedLocation.Value.lon,
                DateTime.UtcNow));
        }
        var result = await _mediator.Send(new GetDriverLocationQuery(id));
        return Ok(result);
    }
    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetLocationHistory(
        Guid id,
        [FromQuery] DateTime? start,
        [FromQuery] DateTime? end)
    {
        var filter = Builders<DriverLocationHistory>.Filter.Eq(h => h.DriverId, id);

        if (start.HasValue)
            filter &= Builders<DriverLocationHistory>.Filter.Gte(h => h.Timestamp, start.Value);

        if (end.HasValue)
            filter &= Builders<DriverLocationHistory>.Filter.Lte(h => h.Timestamp, end.Value);
        try
        {
            _logger.LogInformation($"Getting Driver's Location History between {start} and {end} ");
            var history = await _mongoDbService.GetLocationHistory(filter);
            if (history.Count == 0)
            {
                return NotFound("no history found for this driver between the given duration");
            }
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError("An error has occured while getting location history " + ex.Message);
            return BadRequest("An error has occured while getting location history");
        }
    }

    /// <summary>
    /// Assign a driver based on latitude and longitude.
    /// </summary>
    /// <param name="command">Contains the latitude and longitude.</param>
    /// <returns>The assigned driver ID.</returns>
    [HttpPost("assign")]
    public async Task<IActionResult> AssignDriver([FromBody] AssignDriverCommand command)
    {
        try
        {
            Guid driverId = await _mediator.Send(command);
            return Ok(driverId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign driver");
            return BadRequest("No available drivers!");
        }
    }

    /// <summary>
    /// Find the nearest driver.
    /// </summary>
    /// <param name="lat">Latitude</param>
    /// <param name="lon">Longitude</param>
    /// <returns>The nearest driver ID.</returns>
    [HttpGet("nearest")]
    public async Task<IActionResult> FindNearestDriver([FromQuery] double lat, [FromQuery] double lon)
    {
        try
        {
            Guid driverId = await _mediator.Send(new FindNearestDriverQuery(lat, lon));
            return Ok(driverId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find any drivers");
            return BadRequest("Failed to find any drivers");
        }
    }

}