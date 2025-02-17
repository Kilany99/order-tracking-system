using MediatR;
using OrderService.Infrastructure.Repositories;
using OrderService.Application.Responses;

using OrderService.Application.Features.Orders.Queries;

namespace OrderService.Application.Features.Orders.Handlers
{
    public class GetAllOrdersQueryHandler : IRequestHandler<GetAllOrdersQuery, List<OrderResponse>>
    {
        private readonly IOrderRepository _orderRepository;

        public GetAllOrdersQueryHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<List<OrderResponse>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
        {
            var orders = await _orderRepository.GetAllAsync();
            return orders.Select(order => new OrderResponse(order.Id, order.CustomerId, order.DeliveryAddress, order.Status,order.DeliveryLatitude,order.DeliveryLongitude)).ToList();
        }
    }
}
