using DriverService.Core.Features.Driver.Commands;
using DriverService.Domain.Interfaces;
using DriverService.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DriverService.Core.Features.Driver.Hnadlers;

public class OrderDeliveredCommandHandler: IRequestHandler<OrderDeliveredCommand,Unit>
{

    private readonly IKafkaProducerService _kafkaProducer;
    private readonly ILogger<OrderDeliveredCommandHandler> _logger;
    private readonly IDriverRepository _driverRepository;
    public OrderDeliveredCommandHandler(IKafkaProducerService producerService, ILogger<OrderDeliveredCommandHandler> logger, IDriverRepository driverRepository)
    {
        _kafkaProducer = producerService;
        _logger = logger;
        _driverRepository = driverRepository;
    }

    public async Task<Unit> Handle(OrderDeliveredCommand command, CancellationToken cancellationToken)
    {
       
        var driver = await _driverRepository.GetByIdAsync(command.DriverId);
        // Verify the driver is assigned to this order
        if (driver.CurrentOrderId != command.OrderId)
        {
            _logger.LogError("Driver with ID {driverID} is not assigned to this order Id {orderId}", command.DriverId, command.OrderId);
            return await Unit.Task;
        }
        await _kafkaProducer.ProduceOrderDeliveredEvent(
          command.OrderId, command.DriverId);
        driver.CompleteDelivery();
        await _driverRepository.SaveChangesAsync();
       
       
        return await Unit.Task;
    }
}
