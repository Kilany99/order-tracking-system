using AutoMapper;
using MediatR;
using OrderService.Application.Features.Orders.Queries;
using OrderService.Application.Responses;
using OrderService.Infrastructure.Repositories;
using OrderService.Infrastructure.Services;


namespace OrderService.Application.Features.Orders.Handlers;


public class GetCustomerOrdersQueryHandler : IRequestHandler<GetCustomerOrdersQuery, List<OrderResponse>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMapper _mapper;
    private readonly RedisCacheService _cacheService;

    public GetCustomerOrdersQueryHandler(
        IOrderRepository orderRepository,
        IMapper mapper,
        RedisCacheService cacheService)
    {
        _orderRepository = orderRepository;
        _mapper = mapper;
        _cacheService = cacheService;
    }

    public async Task<List<OrderResponse>> Handle(
        GetCustomerOrdersQuery request,
        CancellationToken cancellationToken)
    {
        // Try to get from cache first
        var cacheKey = $"customer_orders_{request.CustomerId}";
        var cachedOrders = await _cacheService.GetAsync<List<OrderResponse>>(cacheKey);

        if (cachedOrders != null)
        {
            return cachedOrders;
        }

        var orders = await _orderRepository.GetOrdersByCustomerIdAsync(request.CustomerId);
        var response = _mapper.Map<List<OrderResponse>>(orders);

        // Cache the results
        await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5));

        return response;
    }
}