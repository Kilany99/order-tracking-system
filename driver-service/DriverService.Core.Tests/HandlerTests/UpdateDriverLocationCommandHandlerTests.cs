
//This handler updates a driver’s location and integrates with external services (Kafka, Redis, MongoDB).

using Castle.Core.Configuration;
using DriverService.Core.Features.Driver.Commands;
using DriverService.Core.Features.Driver.Hnadlers;
using DriverService.Domain.Entities;
using DriverService.Domain.Interfaces;
using DriverService.Infrastructure.Persistence;
using DriverService.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace DriverService.Core.Tests.HandlerTests;




public class UpdateDriverLocationCommandHandlerTests
{
    
    [Fact]
    public async Task Handle_WhenDriverExists_ShouldUpdateLocationAndNotifyServices()
    {
        // Arrange
        var driver = new Driver { Id = Guid.NewGuid(), CurrentLatitude = 0, CurrentLongitude = 0 };
        var mockRepo = new Mock<IDriverRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(driver.Id)).ReturnsAsync(driver);
        mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Driver>())).Returns(Task.CompletedTask);
        //mockRepo.Setup(r => r.SaveChangesAsync()).Returns((Task.CompletedTask);

        var mockKafka = new Mock<IKafkaProducerService>();
        mockKafka.Setup(k => k.ProduceLocationUpdateAsync(driver.Id, 10.0, 20.0)).Returns(Task.CompletedTask);

        var mockMongo = new Mock<MongoDbContext>();
        mockMongo.Setup(m => m.LocationHistory.InsertOneAsync(It.IsAny<DriverLocationHistory>(), null, CancellationToken.None)).Returns(Task.CompletedTask);

        var mockRedis = new Mock<RedisCacheService>();
        mockRedis.Setup(r => r.CacheDriverLocationAsync(driver.Id, 10.0, 20.0)).Returns(Task.CompletedTask);

        var mockLogger = new Mock<ILogger<UpdateDriverLocationCommandHandler>>();

        var handler = new UpdateDriverLocationCommandHandler(mockRepo.Object, mockKafka.Object, mockMongo.Object, mockLogger.Object, mockRedis.Object);
        var command = new UpdateDriverLocationCommand(driver.Id, 10.0,20.0);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
        mockRepo.Verify(r => r.UpdateAsync(It.Is<Driver>(d => d.CurrentLatitude == 10.0 && d.CurrentLongitude == 20.0)), Times.Once);
        mockKafka.Verify(k => k.ProduceLocationUpdateAsync(driver.Id, 10.0, 20.0), Times.Once);
        mockRedis.Verify(r => r.CacheDriverLocationAsync(driver.Id, 10.0, 20.0), Times.Once);
        mockMongo.Verify(m => m.LocationHistory.InsertOneAsync(It.IsAny<DriverLocationHistory>(), null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDriverDoesNotExist_ShouldThrowDriverNotFoundException()
    {
        // Arrange
        var mockRepo = new Mock<IDriverRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Driver)null);

        var handler = new UpdateDriverLocationCommandHandler(mockRepo.Object, Mock.Of<IKafkaProducerService>(), Mock.Of<MongoDbContext >(), Mock.Of<ILogger<UpdateDriverLocationCommandHandler>>(), Mock.Of<RedisCacheService>());
        var command = new UpdateDriverLocationCommand(Guid.NewGuid(),10.0,20.0);

        // Act & Assert
        await Assert.ThrowsAsync<Domain.Exceptions.DriverNotFoundException>(() => handler.Handle(command, CancellationToken.None));
        mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Driver>()), Times.Never);
    }
}