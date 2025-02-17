using MediatR;
using OrderService.Application.Responses;
using OrderService.Domain.Entities;


namespace OrderService.Application.Features.Orders.Commands;

public record UpdateOrderCommand(
    Guid OrderId,
    string CustomerId,
    string DeliveryAddress,
    OrderStatus Status,
    double DeliveryLatitude,
    double DeliveryLongitude
) : IRequest<ApiResponse<Guid>>;