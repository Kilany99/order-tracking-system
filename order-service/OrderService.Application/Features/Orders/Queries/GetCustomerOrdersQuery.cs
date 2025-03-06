using MediatR;
using OrderService.Application.Responses;


namespace OrderService.Application.Features.Orders.Queries;

public record GetCustomerOrdersQuery(string CustomerId) : IRequest<List<OrderResponse>>;
