using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Features.Orders.Commands;
using OrderService.Application.Responses;
using OrderService.Domain.Entities;
using OrderService.Domain.Models;
using OrderService.Infrastructure.Producers;
using OrderService.Infrastructure.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderService.Application.Features.Orders.Handlers
{
    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, ApiResponse<Guid>>
    {
        private readonly IOrderRepository _repo;
        private readonly ILogger<CreateOrderCommandHandler> _logger;
        private readonly IOrderCreatedProducer _producer;

        public CreateOrderCommandHandler(IOrderRepository repo, ILogger<CreateOrderCommandHandler> logger, IOrderCreatedProducer producer)
        {
            _repo = repo;
            _logger = logger;
            _producer = producer;
        }

        public async Task<ApiResponse<Guid>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);

            var order = Order.Create(
                    request.CustomerId,
                    request.DeliveryAddress,
                    request.DeliveryLatitude,
                    request.DeliveryLongitude);
            await _repo.AddAsync(order);
            _logger.LogInformation("Producing order creating event...");

            var orderEvent = new OrderCreatedEvent(
               order.Id,
               order.CustomerId,
               order.DeliveryLatitude,
               order.DeliveryLongitude
               );
            await _producer.ProduceAsync(orderEvent);

            _logger.LogInformation($"Order created successfully for  {request.CustomerId} of orderID: {order.Id} ");

            return new ApiResponse<Guid>(order.Id, "Order created successfully");
        }
    }
}
