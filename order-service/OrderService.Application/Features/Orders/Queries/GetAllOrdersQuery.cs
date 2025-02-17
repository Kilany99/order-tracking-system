using MediatR;
using OrderService.Application.Responses;

namespace OrderService.Application.Features.Orders.Queries;

public record GetAllOrdersQuery() : IRequest<List<OrderResponse>>;