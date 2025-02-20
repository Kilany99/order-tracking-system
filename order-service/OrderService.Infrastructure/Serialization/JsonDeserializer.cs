using Confluent.Kafka;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Serialization;
public class JsonDeserializer<T> : IDeserializer<T>
{
    private readonly JsonSerializerOptions _options;
    private readonly ILogger<JsonDeserializer<T>> _logger;

    public JsonDeserializer(ILogger<JsonDeserializer<T>> logger = null)
    {
        _logger = logger;
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull)
        {
            LogWarning("Received null data");
            return default;
        }

        try
        {
            var json = Encoding.UTF8.GetString(data.ToArray());
            LogInformation($"Attempting to deserialize: {json}");

            if (string.IsNullOrWhiteSpace(json))
            {
                LogWarning("Received empty JSON string");
                return default;
            }

            // Validate JSON structure
            if (!json.TrimStart().StartsWith("{"))
            {
                LogWarning($"Invalid JSON format received: {json}");
                return default;
            }

            var result = JsonSerializer.Deserialize<T>(json, _options);

            if (result == null)
            {
                LogWarning("Deserialization resulted in null object");
                return default;
            }

            // Additional validation for OrderCreatedEvent
            if (result is OrderCreatedEvent orderEvent)
            {
                if (!orderEvent.IsValid())
                {
                    LogWarning($"Invalid OrderCreatedEvent: {json}");
                    return default;
                }
            }

            LogInformation($"Successfully deserialized to type {typeof(T).Name}");
            return result;
        }
        catch (JsonException ex)
        {
            LogError($"JSON Deserialization Error: {ex.Message}", ex);
            return default;
        }
        catch (Exception ex)
        {
            LogError($"Unexpected error during deserialization: {ex.Message}", ex);
            return default;
        }
    }

    private void LogInformation(string message)
    {
        _logger?.LogInformation(message);
        Console.WriteLine($"INFO: {message}");
    }

    private void LogWarning(string message)
    {
        _logger?.LogWarning(message);
        Console.WriteLine($"WARNING: {message}");
    }

    private void LogError(string message, Exception ex)
    {
        _logger?.LogError(ex, message);
        Console.WriteLine($"ERROR: {message}");
        Console.WriteLine($"Exception: {ex}");
    }
}