using Confluent.Kafka;
using System.Text;
using System.Text.Json;

namespace DriverService.Domain.Serialization;


public class JsonSerializer<T> : ISerializer<T>
{
    private readonly JsonSerializerOptions _options;

    public JsonSerializer()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public byte[] Serialize(T data, SerializationContext context)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, _options);
            Console.WriteLine($"Serialized JSON: {json}");
            return Encoding.UTF8.GetBytes(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Serialization Error: {ex.Message}");
            throw;
        }
    }
}