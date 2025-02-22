using Microsoft.AspNet.SignalR;
using Microsoft.AspNetCore.Mvc;
using OrderService.Infrastructure.Routing.Interfaces;

namespace OrderService.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RoutingController : ControllerBase
{
    private readonly IRoutingService _routingService;
    private readonly ILogger<RoutingController> _logger;

    public RoutingController(
        IRoutingService routingService,
        ILogger<RoutingController> logger)
    {
        _routingService = routingService;
        _logger = logger;
    }

    [HttpGet("route")]
    public async Task<IActionResult> GetRoute(
        [FromQuery] double startLat,
        [FromQuery] double startLng,
        [FromQuery] double endLat,
        [FromQuery] double endLng)
    {
        try
        {
            _logger.LogInformation(
                "Getting route from ({StartLat}, {StartLng}) to ({EndLat}, {EndLng})",
                startLat, startLng, endLat, endLng);

            var route = await _routingService.GetRoute(
                startLat, startLng, endLat, endLng);

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

    [HttpGet("eta")]
    public async Task<IActionResult> GetETA(
        [FromQuery] double startLat,
        [FromQuery] double startLng,
        [FromQuery] double endLat,
        [FromQuery] double endLng)
    {
        try
        {
            _logger.LogInformation(
                "Calculating ETA from ({StartLat}, {StartLng}) to ({EndLat}, {EndLng})",
                startLat, startLng, endLat, endLng);

            var eta = await _routingService.CalculateETA(
                startLat, startLng, endLat, endLng);

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