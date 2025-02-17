
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
    public DriversController(IMediator mediator, IMongoDbService mongoDbService, ILogger<DriversController> logger)
    {
        _mediator = mediator;
        _mongoDbService = mongoDbService;
        _logger = logger;
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
}