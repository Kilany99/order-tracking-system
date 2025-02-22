
using DriverService.Core.Features.Driver.Commands;
using DriverService.Core.Features.Driver.Handlers;
using DriverService.Domain.Entities;
using DriverService.Domain.Exceptions;
using DriverService.Domain.Interfaces;
using DriverService.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace DriverService.Core.Tests;


public class AssignDriverCommandHandlerTests
{
    private readonly Mock<IDriverRepository> _repositoryMock;
    private readonly Mock<IKafkaProducerService> _kafkaProducerMock;
    private readonly Mock<ILogger<AssignDriverCommandHandler>> _loggerMock;
    private readonly AssignDriverCommandHandler _handler;

    public AssignDriverCommandHandlerTests()
    {
        _repositoryMock = new Mock<IDriverRepository>();
        _kafkaProducerMock = new Mock<IKafkaProducerService>();
        _loggerMock = new Mock<ILogger<AssignDriverCommandHandler>>();
        _handler = new AssignDriverCommandHandler(
            _repositoryMock.Object,
            _kafkaProducerMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenOneDriverAvailableAndNear_ShouldAssignDriver()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var command = new AssignDriverCommand(37.7749, -122.4194, orderId);

        var availableDriver = new Driver
        {
            Id = driverId,
            IsAvailable = true,
            CurrentLatitude = 37.7750,  // Very close to order location
            CurrentLongitude = -122.4195
        };

        _repositoryMock.Setup(r => r.FindNearestDriverAsync(command.Latitude, command.Longitude))
            .ReturnsAsync(availableDriver);

        _repositoryMock.Setup(r => r.AssignDriverAsync(driverId, orderId))
            .ReturnsAsync(availableDriver);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(driverId, result);
        _kafkaProducerMock.Verify(
            k => k.ProduceDriverAssignedEvent(orderId, driverId),
            Times.Once);
        _kafkaProducerMock.Verify(
            k => k.ProduceAssignmentFailedEvent(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNearerDriverNotAvailable_ShouldAssignAvailableDriver()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var availableDriverId = Guid.NewGuid();
        var command = new AssignDriverCommand(37.7749, -122.4194, orderId);

        var availableDriver = new Driver
        {
            Id = availableDriverId,
            IsAvailable = true,
            CurrentLatitude = 37.7760,  // Slightly further but available
            CurrentLongitude = -122.4200
        };

        // Simulate repository returning the available driver (even though there might be a closer unavailable one)
        _repositoryMock.Setup(r => r.FindNearestDriverAsync(command.Latitude, command.Longitude))
            .ReturnsAsync(availableDriver);

        _repositoryMock.Setup(r => r.AssignDriverAsync(availableDriverId, orderId))
            .ReturnsAsync(availableDriver);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(availableDriverId, result);
        _kafkaProducerMock.Verify(
            k => k.ProduceDriverAssignedEvent(orderId, availableDriverId),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMultipleDriversAvailable_ShouldAssignNearest()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var nearestDriverId = Guid.NewGuid();
        var command = new AssignDriverCommand(37.7749, -122.4194, orderId);

        var nearestDriver = new Driver
        {
            Id = nearestDriverId,
            IsAvailable = true,
            CurrentLatitude = 37.7750,  // Closest to order location
            CurrentLongitude = -122.4195
        };

        // Repository should return the nearest driver from its internal logic
        _repositoryMock.Setup(r => r.FindNearestDriverAsync(command.Latitude, command.Longitude))
            .ReturnsAsync(nearestDriver);

        _repositoryMock.Setup(r => r.AssignDriverAsync(nearestDriverId, orderId))
            .ReturnsAsync(nearestDriver);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(nearestDriverId, result);
        _kafkaProducerMock.Verify(
            k => k.ProduceDriverAssignedEvent(orderId, nearestDriverId),
            Times.Once);
    }


    [Fact]
    public async Task Handle_WhenDriverAssignmentFails_ShouldThrowAndNotPublishAssignedEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var command = new AssignDriverCommand(37.7749, -122.4194, orderId);

        var availableDriver = new Driver
        {
            Id = driverId,
            IsAvailable = true,
            CurrentLatitude = 37.7750,
            CurrentLongitude = -122.4195
        };

        _repositoryMock.Setup(r => r.FindNearestDriverAsync(command.Latitude, command.Longitude))
            .ReturnsAsync(availableDriver);

        _repositoryMock.Setup(r => r.AssignDriverAsync(driverId, orderId))
            .ThrowsAsync(new DriverNotFoundException(driverId));

        // Act & Assert
        await Assert.ThrowsAsync<DriverNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));

        _kafkaProducerMock.Verify(
            k => k.ProduceDriverAssignedEvent(It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRespectCancellationToken()
    {
        // Arrange
        var command = new AssignDriverCommand(37.7749, -122.4194, Guid.NewGuid());
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _handler.Handle(command, cts.Token));
    }

    [Fact]
    public async Task Handle_WhenNoDriversAvailable_ShouldThrowAndPublishEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var command = new AssignDriverCommand(37.7749, -122.4194, orderId);

        _repositoryMock.Setup(r => r.FindNearestDriverAsync(command.Latitude, command.Longitude))
            .ReturnsAsync((Driver)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NoAvailableDriversException>(
            () => _handler.Handle(command, CancellationToken.None));

        // Verify exception details
        Assert.NotNull(exception);

        // Verify that the failure event was published
        _kafkaProducerMock.Verify(
            k => k.ProduceAssignmentFailedEvent(
                orderId,
                "//CommandHandler//:No available drivers"),
            Times.Once);

        // Verify that no assignment event was published
        _kafkaProducerMock.Verify(
            k => k.ProduceDriverAssignedEvent(It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Never);

        // Verify repository calls
        _repositoryMock.Verify(
            r => r.FindNearestDriverAsync(command.Latitude, command.Longitude),
            Times.Once);
        _repositoryMock.Verify(
            r => r.AssignDriverAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Theory]
    [InlineData(true, "Driver is already assigned")]  // Driver not available
    [InlineData(false, "Driver not found")]          // Driver doesn't exist
    public async Task Handle_WhenDriverAssignmentFails_ShouldThrowAndHandleErrorAppropriately(
        bool driverExists, string expectedErrorMessage)
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var command = new AssignDriverCommand(37.7749, -122.4194, orderId);

        var availableDriver = new Driver
        {
            Id = driverId,
            IsAvailable = true,
            CurrentLatitude = 37.7750,
            CurrentLongitude = -122.4195
        };

        _repositoryMock.Setup(r => r.FindNearestDriverAsync(command.Latitude, command.Longitude))
            .ReturnsAsync(availableDriver);

        if (driverExists)
        {
            // Simulate driver exists but assignment fails
            _repositoryMock.Setup(r => r.AssignDriverAsync(driverId, orderId))
                .ThrowsAsync(new DriverNotAvailableException(driverId));
        }
        else
        {
            // Simulate driver not found
            _repositoryMock.Setup(r => r.AssignDriverAsync(driverId, orderId))
                .ThrowsAsync(new DriverNotFoundException(driverId));
        }

        // Act & Assert
        var exception = driverExists
            ? await Assert.ThrowsAsync<DriverNotAvailableException>(
                () => _handler.Handle(command, CancellationToken.None))
            : await Assert.ThrowsAsync<Exception>(
                () => _handler.Handle(command, CancellationToken.None));

        // Verify exception details
        Assert.Contains(driverId.ToString(), exception.Message);

        // Verify that no assignment event was published
        _kafkaProducerMock.Verify(
            k => k.ProduceDriverAssignedEvent(It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Never);

        // Verify that failure event was published
        _kafkaProducerMock.Verify(
            k => k.ProduceAssignmentFailedEvent(orderId, It.IsAny<string>()),
            Times.Once);

        // Verify repository calls
        _repositoryMock.Verify(
            r => r.FindNearestDriverAsync(command.Latitude, command.Longitude),
            Times.Once);
        _repositoryMock.Verify(
            r => r.AssignDriverAsync(driverId, orderId),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCancellationRequested_ShouldCancelOperationAndCleanup()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var command = new AssignDriverCommand(37.7749, -122.4194, orderId);

        var cts = new CancellationTokenSource();

        var availableDriver = new Driver
        {
            Id = driverId,
            IsAvailable = true,
            CurrentLatitude = 37.7750,
            CurrentLongitude = -122.4195
        };

        // Setup repository to delay and allow cancellation
        _repositoryMock.Setup(r => r.FindNearestDriverAsync(command.Latitude, command.Longitude))
            .Returns(async (double lat, double lon) =>
            {
                await Task.Delay(100, cts.Token); // Add delay to allow cancellation
                return availableDriver;
            });

        // Act
        cts.Cancel(); // Cancel the operation

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _handler.Handle(command, cts.Token));

        // Verify no events were published
        _kafkaProducerMock.Verify(
            k => k.ProduceDriverAssignedEvent(It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Never);
        _kafkaProducerMock.Verify(
            k => k.ProduceAssignmentFailedEvent(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrowsException_ShouldHandleAndPublishFailure()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var command = new AssignDriverCommand(37.7749, -122.4194, orderId);

        _repositoryMock.Setup(r => r.FindNearestDriverAsync(command.Latitude, command.Longitude))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _handler.Handle(command, CancellationToken.None));

        // Verify exception details
        Assert.Contains("Database connection failed", exception.Message);

        // Verify that failure event was published
        _kafkaProducerMock.Verify(
            k => k.ProduceAssignmentFailedEvent(
                orderId,
                It.Is<string>(msg => msg.Contains("Database connection failed"))),
            Times.Once);

        // Verify no assignment event was published
        _kafkaProducerMock.Verify(
            k => k.ProduceDriverAssignedEvent(It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenKafkaProducerFails_ShouldHandleGracefully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var command = new AssignDriverCommand(37.7749, -122.4194, orderId);

        var availableDriver = new Driver
        {
            Id = driverId,
            IsAvailable = true,
            CurrentLatitude = 37.7750,
            CurrentLongitude = -122.4195
        };

        _repositoryMock.Setup(r => r.FindNearestDriverAsync(command.Latitude, command.Longitude))
            .ReturnsAsync(availableDriver);

        _repositoryMock.Setup(r => r.AssignDriverAsync(driverId, orderId))
            .ReturnsAsync(availableDriver);

        _kafkaProducerMock.Setup(k => k.ProduceDriverAssignedEvent(orderId, driverId))
            .ThrowsAsync(new Exception("Kafka connection failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _handler.Handle(command, CancellationToken.None));

        // Verify exception details
        Assert.Contains("Kafka connection failed", exception.Message);

        // Verify repository calls were made
        _repositoryMock.Verify(
            r => r.FindNearestDriverAsync(command.Latitude, command.Longitude),
            Times.Once);
        _repositoryMock.Verify(
            r => r.AssignDriverAsync(driverId, orderId),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOrderAlreayAssigned_ShouldNotAssignDriver()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var driver = _repositoryMock.Object.IsOrderAssignedToAnyDriverAsync(orderId).Result;
        var command = new AssignDriverCommand(37.7749, -122.4194, orderId);
        if (driver != null)
        {
            _repositoryMock.Setup(r => r.AssignDriverAsync(driver.Id, orderId))
            .ThrowsAsync(new InvalidOperationException());

            // Act
            await Assert.ThrowsAsync<InvalidOperationException>(
               () => _handler.Handle(command, CancellationToken.None));

            // Assert
            _repositoryMock.Verify(
                 r => r.AssignDriverAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
                 Times.Never);
            _repositoryMock.Verify(
                 r => r.IsOrderAssignedToAnyDriverAsync(orderId),
                 Times.Once);
        }

    }


    // Custom Exceptions
    public class DriverNotAvailableException : Exception
    {
        public DriverNotAvailableException(Guid driverId)
            : base($"Driver {driverId} is not available for assignment")
        {
        }
    }

    public class DriverNotFoundException : Exception
    {
        public DriverNotFoundException(Guid driverId)
            : base($"Driver {driverId} not found")
        {
        }
    }
}

