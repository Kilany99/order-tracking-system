using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OrderService.API.HealthCheck;


public class KafkaHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public KafkaHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var config = new AdminClientConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"]
            };

            using var adminClient = new AdminClientBuilder(config).Build();
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));

            return metadata.Brokers.Count > 0
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("No brokers available");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(ex.Message);
        }
    }
}