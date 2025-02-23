
using DriverService.Core.Features.Driver.Commands;
using DriverService.Core.Features.Driver.Handlers;
using DriverService.Domain.Entities;
using DriverService.Domain.Interfaces;
using Moq;

namespace DriverService.Core.Tests.HandlerTests;

public class DeleteDriverCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenDriverExists_ShouldDeleteDriver()
    {
        // Arrange
        var driver = new Driver { Id = Guid.NewGuid() };
        var mockRepo = new Mock<IDriverRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(driver.Id)).ReturnsAsync(driver);

        var handler = new DeleteDriverCommandHandler(mockRepo.Object);
        var command = new DeleteDriverCommand(driver.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        mockRepo.Verify(r => r.DeleteAsync(driver.Id), Times.Once);
        mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDriverDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var mockRepo = new Mock<IDriverRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Driver)null);

        var handler = new DeleteDriverCommandHandler(mockRepo.Object);
        var command = new DeleteDriverCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result);
        mockRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        mockRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}