using DriverService.Core.Features.Driver.Commands;
using DriverService.Domain.Exceptions;
using DriverService.Domain.Interfaces;
using DriverService.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Logging;

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
            var driver = await _repository.FindNearestDriverAsync(
                request.Latitude,
                request.Longitude);

            if (driver == null)
            {
                await _kafkaProducer.ProduceAssignmentFailedEvent(
                    request.OrderId,
                    "No available drivers");
                throw new NoAvailableDriversException();
            }
            _logger.LogInformation($"Found nearest driver with Id : {driver.Id} and trying to assign driver...");

            var assignedDriver = await _repository.AssignDriverAsync(driver.Id, request.OrderId);

            await _kafkaProducer.ProduceDriverAssignedEvent(
                request.OrderId,
                assignedDriver.Id);
            _logger.LogInformation($"Driver : {assignedDriver.Id} assinged successfully to order Id: {request.OrderId} ");

            return assignedDriver.Id;
        }
    }
}