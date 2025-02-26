using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OrderService.Domain.Entities;
using OrderService.Domain.Models;

namespace OrderService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ServicePolicy")]
public class CustomerInfoController : ControllerBase
{
    private readonly UserManager<Customer> _userManager;
    private readonly ILogger<CustomerInfoController> _logger;

    public CustomerInfoController(
        UserManager<Customer> userManager,
        ILogger<CustomerInfoController> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet("{customerId}")]
    [ProducesResponseType(typeof(CustomerInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CustomerInfoResponse>> GetCustomerInfo(string customerId)
    {
        try
        {
            var customer = await _userManager.FindByIdAsync(customerId);

            if (customer == null)
            {
                return NotFound(new { message = $"Customer not found with ID: {customerId}" });
            }

            var response = new CustomerInfoResponse
            {
                CustomerId = customer.Id,
                Name = customer.FullName,
                Email = customer.Email
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer info for {CustomerId}", customerId);
            return StatusCode(500, new { message = "An error occurred while processing your request" });
        }
    }

    // Optional: Add an endpoint to get customer by email
    [HttpGet("by-email/{email}")]
    [ProducesResponseType(typeof(CustomerInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CustomerInfoResponse>> GetCustomerInfoByEmail(string email)
    {
        try
        {
            var customer = await _userManager.FindByEmailAsync(email);

            if (customer == null)
            {
                return NotFound(new { message = $"Customer not found with email: {email}" });
            }

            var response = new CustomerInfoResponse
            {
                CustomerId = customer.Id,
                Name = customer.FullName,
                Email = customer.Email
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer info for email {Email}", email);
            return StatusCode(500, new { message = "An error occurred while processing your request" });
        }
    }
}