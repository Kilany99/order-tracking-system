using DriverService.Domain.Entities;
using DriverService.Infrastructure.Persistence;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverService.Infrastructure.Services
{
    public interface IMongoDbService
    {
        Task<List<DriverLocationHistory>> GetLocationHistory(FilterDefinition<DriverLocationHistory> filter);
    }
    public class MongoDbService(MongoDbContext context) : IMongoDbService
    {
        private readonly MongoDbContext _mongoContext = context;

        public async Task<List<DriverLocationHistory>> GetLocationHistory(FilterDefinition<DriverLocationHistory> filter) =>
         await _mongoContext.LocationHistory
               .Find(filter)
               .SortByDescending(h => h.Timestamp)
               .ToListAsync();
        
    }
}
