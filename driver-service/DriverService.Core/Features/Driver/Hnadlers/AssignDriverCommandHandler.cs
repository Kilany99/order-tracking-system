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

                // Check existing assignment
                var existingDriver = await _repository.IsOrderAssignedToAnyDriverAsync(request.OrderId);
                if (existingDriver != null)
                {
                    _logger.LogInformation(
                        "Order {OrderId} is already assigned to driver {DriverId}",
                        request.OrderId,
                        existingDriver.Id);

                    // Instead of throwing an exception, return the existing driver's ID
                    return existingDriver.Id;
                }

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

                using var transaction = await _repository.BeginTransactionAsync();
                try
                {
                    var assignedDriver = await _repository.AssignDriverAsync(driver.Id, request.OrderId);
                    assignedDriver.AcceptOrder(request.OrderId);
                    await _repository.SaveChangesAsync();

                    await _kafkaProducer.ProduceDriverAssignedEvent(
                        request.OrderId,
                        assignedDriver.Id,
                        assignedDriver.Name);

                    await transaction.CommitAsync();

                    _logger.LogInformation(
                        "Driver {DriverId} assigned successfully to order {OrderId}",
                        assignedDriver.Id,
                        request.OrderId);

                    return assignedDriver.Id;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _kafkaProducer.ProduceAssignmentFailedEvent(
                        request.OrderId,
                        ex.Message);
                    throw;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex,
                    "Error assigning driver to order {OrderId}",
                    request.OrderId);
                throw;
            }
        }
    }
}