using DriverService.Core.Features.Driver.Commands;
using DriverService.Domain.Exceptions;
using DriverService.Domain.Interfaces;
using DriverService.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace DriverService.Core.Features.Driver.Handlers
{
    public class AssignDriverCommandHandler : IRequestHandler<AssignDriverCommand, Guid>
    {
        private readonly IDriverRepository _repository;
        private readonly IKafkaProducerService _kafkaProducer;
        private readonly ILogger<AssignDriverCommandHandler> _logger;

        public AssignDriverCommandHandler(
            IDriverRepository repository,
            IKafkaProducerService kafkaProducer,
            ILogger<AssignDriverCommandHandler> logger)
        {
            _repository = repository;
            _kafkaProducer = kafkaProducer;
            _logger = logger;
        }

        public async Task<Guid> Handle(AssignDriverCommand request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                // checks if the order assigned already to any driver
                var Isdriver = await _repository.IsOrderAssignedToAnyDriverAsync(request.OrderId);
                if(Isdriver != null)
                    throw new InvalidOperationException($"This order is assigned already to driverId : {Isdriver.Id}");
                var driver = await _repository.FindNearestDriverAsync(
                request.Latitude,
                request.Longitude);

                if (driver == null)
                {
                    await _kafkaProducer.ProduceAssignmentFailedEvent(
                        request.OrderId,
                        "//CommandHandler//:No available drivers");
                    throw new NoAvailableDriversException();
                }
                _logger.LogInformation($"Found nearest driver with Id : {driver.Id} and trying to assign driver...");
                try
                {
                    var assignedDriver = await _repository.AssignDriverAsync(driver.Id, request.OrderId);
                    assignedDriver.AcceptOrder(request.OrderId);
                    await _repository.SaveChangesAsync();
                    await _kafkaProducer.ProduceDriverAssignedEvent(
                        request.OrderId,
                        assignedDriver.Id,
                        assignedDriver.Name);
                    _logger.LogInformation($"Driver : {assignedDriver.Id} assinged successfully to order Id: {request.OrderId} ");

                    return assignedDriver.Id;
                }
                catch (Exception ex)
                {
                    await _kafkaProducer.ProduceAssignmentFailedEvent(
                        request.OrderId,
                        ex.Message);
                    throw;
                }

            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
    }
}