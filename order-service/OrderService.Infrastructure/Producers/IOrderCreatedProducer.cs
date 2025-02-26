using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderService.Infrastructure.Serialization;
using System.Text.Json;
using OrderService.Domain.Models;

namespace OrderService.Infrastructure.Producers
{
    public interface IOrderCreatedProducer
    {
        Task ProduceAsync(OrderCreatedEvent orderEvent);
    }

    public class OrderCreatedProducer : IOrderCreatedProducer
    {
        private readonly IProducer<string, OrderCreatedEvent> _producer;
        private readonly ILogger<OrderCreatedProducer> _logger;

        public OrderCreatedProducer(IConfiguration config, ILogger<OrderCreatedProducer> logger)
        {
            _logger = logger;

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = config["kafka:BootstrapServers"],
                EnableIdempotence = true,
                MessageSendMaxRetries = 3,
                Acks = Acks.All
            };

            _producer = new ProducerBuilder<string, OrderCreatedEvent>(producerConfig)
                .SetValueSerializer(new JsonSerializer<OrderCreatedEvent>())
                .Build();
        }

        public async Task ProduceAsync(OrderCreatedEvent orderEvent)
        {
            try
            {
                var message = new Message<string, OrderCreatedEvent>
                {
                    Key = orderEvent.OrderId.ToString(),
                    Value = orderEvent
                };

                var deliveryReport = await _producer.ProduceAsync("orders-created", message);

                _logger.LogInformation("Produced order created event for order {OrderId} to {TopicPartitionOffset}",
                    orderEvent.OrderId, deliveryReport.TopicPartitionOffset);
            }
            catch (ProduceException<string, OrderCreatedEvent> ex)
            {
                _logger.LogError(ex, "Failed to deliver order created event for {OrderId}", orderEvent.OrderId);
                throw;
            }
        }

        public void Dispose()
        {
            _producer.Flush(TimeSpan.FromSeconds(5));
            _producer.Dispose();
        }
    }
}