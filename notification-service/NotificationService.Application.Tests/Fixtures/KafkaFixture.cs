
using Testcontainers.Kafka;

namespace NotificationService.Application.Tests.Fixtures;


public class KafkaFixture : IAsyncLifetime
{
    public KafkaContainer KafkaContainer { get; }

    public KafkaFixture()
    {
        KafkaContainer = new KafkaBuilder()
            .WithImage("confluentinc/cp-kafka:7.4.0")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await KafkaContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await KafkaContainer.DisposeAsync();
    }
}