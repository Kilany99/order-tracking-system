using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Features.Routes.Commands;
using OrderService.Application.Features.Routes.Queries;

namespace OrderService.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RoutingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<RoutingController> _logger;

    public RoutingController(
        IMediator mediator,
        ILogger<RoutingController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("route")]
    public async Task<IActionResult> GetRoute([FromBody] GetRouteQuery query)
    {
        try
        {
            if (query == null)
            {
                return BadRequest("Query parameters are required");
            }

            _logger.LogInformation(
                "Getting route from ({StartLat}, {StartLng}) to ({EndLat}, {EndLng})",
                query.startLat, query.startLng, query.endLat, query.endLng);

            var route = await _mediator.Send(query);

            _logger.LogInformation(
                "Route found with {Points} points, distance: {Distance}m, duration: {Duration}s",
                route.RoutePoints.Count,
                route.Distance,
                route.Duration);

            return Ok(route);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting route");
            return StatusCode(500, new { message = "Failed to get route", error = ex.Message });
        }
    }

    // Alternative GET endpoint with query parameters
    [HttpGet("route")]
    public async Task<IActionResult> GetRouteGet(
        [FromQuery] double startLat,
        [FromQuery] double startLng,
        [FromQuery] double endLat,
        [FromQuery] double endLng)
    {
        try
        {
            var query = new GetRouteQuery(
                startLat,
                startLng,
                endLat,
                endLng
            );

            _logger.LogInformation(
                "Getting route from ({StartLat}, {StartLng}) to ({EndLat}, {EndLng})",
                startLat, startLng, endLat, endLng);

            var route = await _mediator.Send(query);

            _logger.LogInformation(
                "Route found with {Points} points, distance: {Distance}m, duration: {Duration}s",
                route.RoutePoints.Count,
                route.Distance,
                route.Duration);

            return Ok(route);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting route");
            return StatusCode(500, new { message = "Failed to get route", error = ex.Message });
        }
    }

    [HttpPost("eta")]
    public async Task<IActionResult> GetETA([FromBody] CalculateETACommand command)
    {
        try
        {
            if (command == null)
            {
                return BadRequest("Command parameters are required");
            }

            _logger.LogInformation(
                "Calculating ETA from ({StartLat}, {StartLng}) to ({EndLat}, {EndLng})",
                command.startLat, command.startLng, command.endLat, command.endLng);

            var eta = await _mediator.Send(command);

            _logger.LogInformation("ETA calculated: {Eta}", eta);

            return Ok(new
            {
                eta = eta.TotalSeconds,
                formattedEta = eta.ToString(@"hh\:mm\:ss")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating ETA");
            return StatusCode(500, new { message = "Failed to calculate ETA", error = ex.Message });
        }
    }
}