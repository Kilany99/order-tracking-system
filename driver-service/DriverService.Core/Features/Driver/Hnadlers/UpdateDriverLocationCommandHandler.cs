using DnsClient.Internal;
using DriverService.Core.Features.Driver.Commands;
using DriverService.Domain.Entities;
using DriverService.Domain.Exceptions;
using DriverService.Domain.Interfaces;
using DriverService.Infrastructure.Persistence;
using DriverService.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DriverService.Core.Features.Driver.Hnadlers;

public class UpdateDriverLocationCommandHandler 
    : IRequestHandler<UpdateDriverLocationCommand, Unit>
{
    private readonly IDriverRepository _repository;
    private readonly IKafkaProducerService _kafkaService;
    private readonly MongoDbContext _mongoContext;
    private readonly ILogger<UpdateDriverLocationCommandHandler> _logger;
    private readonly RedisCacheService _redisCacheService;
    public UpdateDriverLocationCommandHandler(
        IDriverRepository repository,
        IKafkaProducerService kafkaService,
        MongoDbContext mongoContext,
        ILogger<UpdateDriverLocationCommandHandler> logger,
        RedisCacheService redisCacheService)
    {
        _repository = repository;
        _kafkaService = kafkaService;
        _mongoContext = mongoContext;
        _logger = logger;
        _redisCacheService = redisCacheService;
    }

    public async Task<Unit> Handle(
        UpdateDriverLocationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var driver = await _repository.GetByIdAsync(request.DriverId)
                ?? throw new DriverNotFoundException(request.DriverId);

            // Update location in database
            driver.CurrentLatitude = request.Latitude;
            driver.CurrentLongitude = request.Longitude;
            driver.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(driver);
            await _repository.SaveChangesAsync();

            // Publish to Kafka
            await _kafkaService.ProduceLocationUpdateAsync(
                request.DriverId,
                request.Latitude,
                request.Longitude);

            // Update Redis cache
            _logger.LogInformation("caching location ...");

            await _redisCacheService.CacheDriverLocationAsync(
                request.DriverId,
                request.Latitude,
                request.Longitude);

            // Store in MongoDB
            _logger.LogInformation("saving location history in mongo db...");

            await _mongoContext.LocationHistory.InsertOneAsync(new DriverLocationHistory
            {
                DriverId = driver.Id,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogInformation("Update command finished successfully.");

            return Unit.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occured while handling update command: {ex.Message}");
            return Unit.Value;
        }
    }
}