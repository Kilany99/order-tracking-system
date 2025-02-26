using DriverService.Core.Features.Driver.Commands;
using DriverService.Domain.Entities;
using DriverService.Domain.Interfaces;
using DriverService.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace DriverService.Core.Features.Driver.Hnadlers;

public class OrderPickupCommandHandler: IRequestHandler<OrderPickupCommand,Unit>
{
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly ILogger<OrderPickupCommandHandler> _logger;
    private readonly IDriverRepository _driverRepository;
    public OrderPickupCommandHandler(IKafkaProducerService producerService, ILogger<OrderPickupCommandHandler> logger,IDriverRepository driverRepository)
    {
        _kafkaProducer = producerService;
        _logger = logger;
        _driverRepository = driverRepository;
    }

    public async Task<Unit> Handle(OrderPickupCommand command,CancellationToken cancellationToken)
    {
        // Verify the driver is assigned to this order
        var isDriverAssigned = await _driverRepository.IsOrderAssignedToAnyDriverAsync(command.OrderId);
        if (isDriverAssigned?.Id != command.DriverId)
        {
            _logger.LogError("Driver with ID {driverID} is not assigned to this order Id {orderId}",command.DriverId,command.OrderId);
            return await Unit.Task;
        }
        var driver = await _driverRepository.GetByIdAsync(command.DriverId);
        driver.StartDelivery();
        await _driverRepository.SaveChangesAsync();
        await _kafkaProducer.ProduceOrderPickedUpEvent(
            command.OrderId, command.DriverId, command.DriverName);
        return await Unit.Task;
    }
}
