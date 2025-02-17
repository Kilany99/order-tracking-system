using MediatR;


namespace OrderService.Application.Features.Orders.Commands;

public record DeleteOrderCommand(Guid Id) : IRequest<bool>;
