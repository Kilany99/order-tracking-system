using DriverService.Core.Features.Driver.Commands;
using DriverService.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
namespace DriverService.API.Controllers;
/// <summary>
/// Used for drivers auth.
/// </summary>
///
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;
    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Command to create a new Driver.
    /// </summary>
    ///

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDriverCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(new { Message = "Driver registered successfully", Driver = result });
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occured during driver registration."+ ex.Message);
            return BadRequest("An error occured during driver registration." + ex.Message);
        }
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDriverCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (InvalidCredentialsException)
        {
            return Unauthorized("Invalid email or password");
        }
        catch (DriverNotFoundException)
        {
            return NotFound("Driver not found");
        }
        catch(Exception)
        {
            return BadRequest("An error occured while logging in");
        }
    }
}