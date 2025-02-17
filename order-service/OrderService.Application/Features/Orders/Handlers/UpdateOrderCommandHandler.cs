using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Features.Orders.Commands;
using OrderService.Application.Responses;
using OrderService.Domain.Exceptions;
using OrderService.Infrastructure.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderService.Application.Features.Orders.Handlers
{
    public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand, ApiResponse<Guid>>
    {
        private readonly IOrderRepository _repo;
        private readonly ILogger<UpdateOrderCommandHandler> _logger;

        public UpdateOrderCommandHandler(IOrderRepository repo, ILogger<UpdateOrderCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<ApiResponse<Guid>> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating order {OrderId}", request.OrderId);

            var order = await _repo.GetByIdAsync(request.OrderId);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found", request.OrderId);
                return new ApiResponse<Guid>(Guid.Empty, "Order not found");
            }

            if (!string.IsNullOrEmpty(request.CustomerId))
                order.UpdateCustomerId(request.CustomerId);

            if (!string.IsNullOrEmpty(request.DeliveryAddress))
                order.UpdateDeliveryAddress(request.DeliveryAddress);

            order.UpdateStatus(request.Status);
            await _repo.UpdateAsync(order);

            _logger.LogInformation("Order {OrderId} updated successfully", request.OrderId);
            return new ApiResponse<Guid>(order.Id, "Order updated successfully");
        }
    }
}
