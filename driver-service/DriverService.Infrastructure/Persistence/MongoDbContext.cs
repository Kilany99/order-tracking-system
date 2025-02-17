using DriverService.Domain.Entities;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace DriverService.Infrastructure.Persistence;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IConfiguration config)
    {
        var connectionString = config["MongoDb:ConnectionString"];
        var databaseName = config["MongoDb:DatabaseName"];

        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentNullException("MongoDb:ConnectionString", "MongoDb connection string is missing.");
        if (string.IsNullOrEmpty(databaseName))
            throw new ArgumentNullException("MongoDb:DatabaseName", "MongoDb database name is missing.");

        //BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }


    public IMongoCollection<DriverLocationHistory> LocationHistory =>
        _database.GetCollection<DriverLocationHistory>("location_history");
}