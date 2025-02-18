
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using DriverService.Domain.Events;
using Microsoft.Extensions.Logging;
using DnsClient.Internal;
using System.Text;
using System.Text.Json;
using DriverService.Domain.Serialization;
namespace DriverService.Infrastructure.Services;

public  interface IKafkaProducerService
{
    Task ProduceLocationUpdateAsync(Guid driverId, double lat, double lon);
    Task ProduceAssignmentFailedEvent(Guid orderId, string reason);
    Task ProduceDriverAssignedEvent(Guid orderId, Guid driverId);
}
public class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly IProducer<string, DriverAssignedEvent> _driverAssignedProducer;
    private readonly IProducer<string, OrderAssignmentFailedEvent> _assignmentFailedProducer;
    public KafkaProducerService(
        IConfiguration config,
        ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"],
            Acks = Acks.All,
            MessageTimeoutMs = 10000,
            EnableIdempotence = true,
            MaxInFlight = 5,          
            LingerMs = 5,              // Improves batching efficiency
            BatchSize = 65536,         // Adjust based on throughput needs
            CompressionType = CompressionType.Lz4, // Improves performance
            MessageSendMaxRetries = 5


        };

        _producer = new ProducerBuilder<string, string>(producerConfig)
            .SetErrorHandler((_, e) =>
                _logger.LogError($"Kafka error: {e.Reason}"))
            .SetLogHandler((_, message) =>
                _logger.LogInformation($"Kafka log: {message.Message}"))
            .Build();
        _driverAssignedProducer = new ProducerBuilder<string, DriverAssignedEvent>(producerConfig)
              .SetValueSerializer(new JsonSerializer<DriverAssignedEvent>())
              .Build();

        _assignmentFailedProducer = new ProducerBuilder<string, OrderAssignmentFailedEvent>(producerConfig)
            .SetValueSerializer(new JsonSerializer<OrderAssignmentFailedEvent>())
            .Build();


        _topic = config["Kafka:Topic"];
    }

    public async Task ProduceLocationUpdateAsync(
        Guid driverId,
        double lat,
        double lon)
    {
        _logger.LogInformation($"producing location update for {driverId.ToString()}");
        try
        {

            var message = new Message<string, string>
            {
                Key = driverId.ToString(),
                Value = JsonSerializer.Serialize(new
                {
                    DriverId = driverId,
                    Latitude = lat,
                    Longitude = lon,
                    Timestamp = DateTime.UtcNow
                })
            };
            _logger.LogInformation(message.Value + "  " + _topic);
           var deliveryReport = await _producer.ProduceAsync(_topic, message);
            _logger.LogInformation(
                $"Delivered to: {deliveryReport.TopicPartitionOffset}");
        }
        catch (ProduceException<string, string> e)
        {
            _logger.LogError($"Delivery failed: {e.Error.Reason}");
            throw;
        }
    }

    public async Task ProduceDriverAssignedEvent(Guid orderId, Guid driverId)
    {
        try
        {
            _logger.LogInformation($"Publishing driver assign event...");
            await _driverAssignedProducer.ProduceAsync("drivers-assigned", new Message<string, DriverAssignedEvent>
            {
                Key = orderId.ToString(),
                Value = new DriverAssignedEvent(orderId, driverId, DateTime.UtcNow)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish driver assignment event");
        }
    }

    public async Task ProduceAssignmentFailedEvent(Guid orderId, string reason)
    {
        try
        {
            await _assignmentFailedProducer.ProduceAsync("order-assignment-failed",
                new Message<string, OrderAssignmentFailedEvent>
                {
                    Key = orderId.ToString(),
                    Value = new OrderAssignmentFailedEvent(orderId, reason, DateTime.UtcNow)
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish assignment failed event");
        }
    }

    public void Dispose()
    {
        _driverAssignedProducer.Flush(TimeSpan.FromSeconds(5));
        _driverAssignedProducer.Dispose();
        _assignmentFailedProducer.Dispose();
        _producer?.Dispose();

    }
}