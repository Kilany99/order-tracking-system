
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using DriverService.Domain.Events;
using Microsoft.Extensions.Logging;
using DnsClient.Internal;
using System.Text;
using DriverService.Domain.Serialization;
using DriverService.Infrastructure.DriversMetrics;
namespace DriverService.Infrastructure.Services;

public  interface IKafkaProducerService
{
    Task ProduceLocationUpdateAsync(Guid driverId, double lat, double lon);
    Task ProduceAssignmentFailedEvent(Guid orderId, string reason);
    Task ProduceDriverAssignedEvent(Guid orderId, Guid driverId,string driverName);
    Task ProduceOrderPickedUpEvent(Guid orderId, Guid driverId, string driverName);
    Task ProduceOrderDeliveredEvent(Guid orderId, Guid driverId);


}
public class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _locationTopic;
    private readonly string _driverAssignedTopic;
    private readonly string _assignmentFailedTopic;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly IProducer<string, DriverAssignedEvent> _driverAssignedProducer;
    private readonly IProducer<string, OrderAssignmentFailedEvent> _assignmentFailedProducer;
    private readonly IProducer<string, OrderDeliveredEvent> _deliveredProducer;
    private readonly IProducer<string, OrderPickedUpEvent> _orderPickedUpProducer;
    private readonly string _orderPickedUpTopic;
    private readonly string _orderDeliveredTopic;

    private readonly DriverMetrics _metrics;

    public KafkaProducerService(
        IConfiguration config,
        ILogger<KafkaProducerService> logger,
        DriverMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
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

        _orderPickedUpProducer = new ProducerBuilder<string, OrderPickedUpEvent>(producerConfig)
          .SetValueSerializer(new JsonSerializer<OrderPickedUpEvent>())
          .Build();
        _deliveredProducer = new ProducerBuilder<string, OrderDeliveredEvent>(producerConfig)
            .SetValueSerializer(new JsonSerializer<OrderDeliveredEvent>())
            .Build();


        // Read topics from configuration
        _locationTopic = config["Kafka:Topic"];               
        _driverAssignedTopic = config["Kafka:DriverAssigned"];  
        _assignmentFailedTopic = config["Kafka:AssignmentFailed"];
        _orderPickedUpTopic = config["Kafka:OrderPickedUp"];
        _orderDeliveredTopic = config["Kafka:OrderDelivered"];
        _logger.LogInformation("Kafka producer is running...");
    }

    public async Task ProduceLocationUpdateAsync(
        Guid driverId,
        double lat,
        double lon)
    {
        var startTime = DateTime.UtcNow;
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
            _logger.LogInformation(message.Value + "  " + _locationTopic);
           var deliveryReport = await _producer.ProduceAsync(_locationTopic, message);
            _logger.LogInformation(
                $"Delivered to: {deliveryReport.TopicPartitionOffset}");
            _metrics.RecordLocationUpdate((DateTime.UtcNow - startTime).TotalSeconds);

        }
        catch (ProduceException<string, string> e)
        {
            _logger.LogError($"Delivery failed: {e.Error.Reason}");
            throw;
        }
    }

    public async Task ProduceDriverAssignedEvent(Guid orderId, Guid driverId,string driverName)
    {
        try
        {
            _logger.LogInformation($"Publishing driver assign event...");
            await _driverAssignedProducer.ProduceAsync(_driverAssignedTopic, new Message<string, DriverAssignedEvent>
            {
                Key = orderId.ToString(),
                Value = new DriverAssignedEvent(orderId, driverId, driverName ,DateTime.UtcNow)
            });
            _metrics.RecordDriverAssignment(true);
        }
        catch (Exception ex)
        {
            _metrics.RecordDriverAssignment(false);
            _logger.LogError(ex, "Failed to publish driver assignment event");
        }
    }

    public async Task ProduceAssignmentFailedEvent(Guid orderId, string reason)
    {
        try
        {
            await _assignmentFailedProducer.ProduceAsync(_assignmentFailedTopic,
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

    public async Task ProduceOrderPickedUpEvent(Guid orderId, Guid driverId, string driverName)
    {
        try
        {
            _logger.LogInformation("Publishing order pickup event for order {OrderId}", orderId);

            await _orderPickedUpProducer.ProduceAsync(
                _orderPickedUpTopic,
                new Message<string, OrderPickedUpEvent>
                {
                    Key = orderId.ToString(),
                    Value = new OrderPickedUpEvent(
                        orderId,
                        driverId,
                        driverName,
                        DateTime.UtcNow
                    )
                });

            _logger.LogInformation("Successfully published order pickup event for order {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish order pickup event for order {OrderId}", orderId);
            throw;
        }
    }
    public async Task ProduceOrderDeliveredEvent(Guid orderId, Guid driverId)
    {
        try
        {
            _logger.LogInformation("Publishing order delivered event for order {OrderId}", orderId);

            await _deliveredProducer.ProduceAsync(
                _orderDeliveredTopic,
                new Message<string, OrderDeliveredEvent>
                {
                    Key = orderId.ToString(),
                    Value = new OrderDeliveredEvent(
                        orderId,
                        driverId,
                        DateTime.UtcNow
                    )
                });

            _logger.LogInformation("Successfully published order delivered event for order {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish order delivered event for order {OrderId}", orderId);
            throw;
        }

    }
    public void Dispose()
    {
        _driverAssignedProducer.Flush(TimeSpan.FromSeconds(5));
        _driverAssignedProducer.Dispose();
        _assignmentFailedProducer.Dispose();
        _orderPickedUpProducer.Dispose();
        _producer?.Dispose();

    }
}