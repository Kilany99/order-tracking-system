using Confluent.Kafka;
using System.Text.Json;

namespace OrderService.API.Serialization;


public class JsonDeserializer<T> : IDeserializer<T>
{
    private readonly JsonSerializerOptions _options;

    public JsonDeserializer()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
    }

    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull)
            return default;

        return JsonSerializer.Deserialize<T>(data, _options);
    }
}
