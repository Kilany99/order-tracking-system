using MediatR;
using OrderService.Application.Responses;


namespace OrderService.Application.Features.Orders.Commands;


public record CreateOrderCommand(
    string CustomerId,
    string DeliveryAddress,
    double DeliveryLatitude,
    double DeliveryLongitude) : IRequest<ApiResponse<Guid>>;

