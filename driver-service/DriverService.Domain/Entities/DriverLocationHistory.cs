
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DriverService.Domain.Entities;

public class DriverLocationHistory
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    [BsonRepresentation(BsonType.String)]
    public Guid DriverId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime Timestamp { get; set; }
}