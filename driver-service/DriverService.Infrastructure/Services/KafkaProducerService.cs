
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using DriverService.Domain.Events;
using Microsoft.Extensions.Logging;
using DnsClient.Internal;
namespace DriverService.Infrastructure.Services;

public  interface IKafkaProducerService
{
    Task ProduceLocationUpdateAsync(Guid driverId, double lat, double lon);
}
public class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;
    private readonly ILogger<KafkaProducerService> _logger;

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

    public void Dispose() => _producer?.Dispose();
}