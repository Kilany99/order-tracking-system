
namespace OrderService.Infrastructure.Services
{
    public interface ICustomConnectionManager
    {
        void AddConnection(string orderId, string connectionId);
        string GetOrderId(string connectionId);
        void RemoveConnection(string orderId, string connectionId);
    }
    public class CustomConnectionManager : ICustomConnectionManager
    {
        // Using a dictionary for simplicity; consider thread-safe collections for concurrent access
        private readonly Dictionary<string, string> _connectionMap = new();

        public void AddConnection(string orderId, string connectionId)
        {
            // You might want to store multiple connections per order; this is just a simple example.
            _connectionMap[connectionId] = orderId;
        }

        public string GetOrderId(string connectionId)
        {
            return _connectionMap.TryGetValue(connectionId, out var orderId)
                ? orderId
                : "";
        }

        public void RemoveConnection(string orderId, string connectionId)
        {
            _connectionMap.Remove(connectionId);
        }
    }

}
