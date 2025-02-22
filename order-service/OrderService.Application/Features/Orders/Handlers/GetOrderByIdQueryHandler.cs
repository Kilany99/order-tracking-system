using MediatR;
using OrderService.Application.Features.Orders.Queries;
using OrderService.Application.Responses;
using OrderService.Domain;
using OrderService.Infrastructure.Repositories;


namespace OrderService.Application.Features.Orders.Handlers;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderResponse> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.Id) ?? throw new Exception("Order not found!");
        return order.DriverId.HasValue
            ? new OrderResponse(
                id: order.Id,
                driverId: order.DriverId.Value,
                cutomerId: order.CustomerId,
                delvAddress: order.DeliveryAddress,
                orderStatus: order.Status,
                latitude: order.DeliveryLatitude,
                longitude: order.DeliveryLongitude)
            : new OrderResponse(
                id: order.Id,
                cutomerId: order.CustomerId,
                delvAddress: order.DeliveryAddress,
                orderStatus: order.Status,
                latitude: order.DeliveryLatitude,
                longitude: order.DeliveryLongitude);
    }
}
