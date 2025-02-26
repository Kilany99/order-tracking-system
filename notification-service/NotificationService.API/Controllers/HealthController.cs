using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;

namespace NotificationService.API.Controllers;


[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IConfiguration configuration, ILogger<HealthController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("kafka")]
    public IActionResult CheckKafkaConnection()
    {
        try
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"]
            }).Build();

            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));

            return Ok(new
            {
                Status = "Healthy",
                BrokerCount = metadata.Brokers.Count,
                Topics = metadata.Topics.Select(t => t.Topic)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kafka health check failed");
            return StatusCode(500, new { Status = "Unhealthy", Error = ex.Message });
        }
    }
}