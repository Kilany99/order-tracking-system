using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Features.Orders.Commands;
using OrderService.Domain.Exceptions;
using OrderService.Infrastructure.Repositories;


namespace OrderService.Application.Features.Orders.Handlers;

public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand,Guid>
{
    private readonly IOrderRepository _repo;
    private readonly ILogger<UpdateOrderStatusCommand> _logger;

    public UpdateOrderStatusCommandHandler(IOrderRepository repo, ILogger<UpdateOrderStatusCommand> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<Guid> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating order {OrderId}", request.OrderId);
        var order = await _repo.GetByIdAsync(request.OrderId)??
            throw new OrderDomainException("Order not found");

        order.UpdateStatus(request.Status); 
        await _repo.UpdateAsync(order);

        _logger.LogInformation("Order {OrderId} status updated successfully.", order.Id);
        // Return the updated order's ID
        return order.Id;
    }
}