using AutoMapper;
using MediatR;
using OrderService.Application.Features.Orders.Queries;
using OrderService.Application.Responses;
using OrderService.Infrastructure.Repositories;


namespace OrderService.Application.Features.Orders.Handlers;

public class GetActiveCustomerOrdersQueryHandler : IRequestHandler<GetActiveCustomerOrdersQuery, List<OrderResponse>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMapper _mapper;

    public GetActiveCustomerOrdersQueryHandler(IOrderRepository orderRepository, IMapper mapper)
    {
        _orderRepository = orderRepository;
        _mapper = mapper;
    }

    public async Task<List<OrderResponse>> Handle(GetActiveCustomerOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetActiveOrdersByCustomerIdAsync(request.CustomerId);
        return _mapper.Map<List<OrderResponse>>(orders);
    }
}