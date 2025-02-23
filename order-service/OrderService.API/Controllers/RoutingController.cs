using MediatR;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Features.Routes.Commands;
using OrderService.Application.Features.Routes.Queries;
using OrderService.Infrastructure.Routing.Interfaces;

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

    [HttpGet("route")]
    public async Task<IActionResult> GetRoute(GetRouteQuery query)
    {
        try
        {
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

    [HttpGet("eta")]
    public async Task<IActionResult> GetETA(CalculateETACommand command)
    {
        try
        {
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