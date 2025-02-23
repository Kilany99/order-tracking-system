using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Features.Orders.Commands;
using OrderService.Application.Features.Orders.Queries;
using OrderService.Application.Responses;
using OrderService.Domain;
using System.Security.Claims;

namespace OrderService.API.Controllers;
/// <summary>
/// Service endpoint for managing orders.
/// </summary>
[Authorize]
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IMediator mediator, ILogger<OrdersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }


    /// <summary>
    /// Command to create a new order.
    /// </summary>
    /// <example>
    /// {
    ///   "customerId": "cust-123",
    ///   "deliveryAddress": "123 Main St",
    ///   "deliveryLatitude": 30.0444,
    ///   "deliveryLongitude": 31.2357
    /// }
    /// </example>
    [HttpPost]
    public async Task<ApiResponse<Guid>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var customerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(customerId))
        {
            var response = new ApiResponse<Guid>(Guid.Empty,"Unauthorized 401");
            return response;
        }
        _logger.LogInformation("Verified cumstomer with customer ID : {customerId}",customerId);
        var command = new CreateOrderCommand(customerId, request.DeliveryAddress, request.DeliveryLatitude, request.DeliveryLongitude);
        var orderId = await _mediator.Send(command);
        return orderId;
    }

    /// <summary>Gets an order with its guide.</summary>
    [HttpGet("{id}")]
    public async Task<OrderResponse> GetOrder(Guid id)
    {
        var order = await _mediator.Send(new GetOrderByIdQuery(id));
        return order;
    }
    /// <summary>Updates order status.</summary>

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusCommand command)
    {
        if (id != command.OrderId)
            return BadRequest("Order ID mismatch");

        await _mediator.Send(command);
        return NoContent();
    }
    /// <summary>Updates an order.</summary>

    [HttpPut("{id}")]
    public async Task<ApiResponse<Guid>> UpdateOrder(Guid id, [FromBody] UpdateOrderCommand command)
    {
        if (id != command.OrderId)
            return new ApiResponse<Guid>(Guid.Empty, "Id is not corrct!");

        var result = await _mediator.Send(command);
        return result;
    }
    /// <summary>Deletes an order.</summary>

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteOrder(Guid id)
    {
        var result = await _mediator.Send(new DeleteOrderCommand(id));
        return Ok(new ApiResponse<bool>(result, result ? "Order deleted successfully" : "Order not found"));
    }
    /// <summary>Gets a list of order.</summary>

    [HttpGet("get-all")]
    public async Task<ActionResult<ApiResponse<List<OrderResponse>>>> ListOrders()
    {
        var orders = await _mediator.Send(new GetAllOrdersQuery());
        return Ok(new ApiResponse<List<OrderResponse>>(orders, "Orders fetched successfully"));
    }

}