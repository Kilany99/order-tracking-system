using MediatR;
using OrderService.Application.Responses;
using OrderService.Domain;

namespace OrderService.Application.Features.Orders.Queries;

public record GetOrderByIdQuery(Guid Id) : IRequest<OrderResponse>;

