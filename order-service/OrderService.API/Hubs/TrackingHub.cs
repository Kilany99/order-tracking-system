using Microsoft.AspNetCore.SignalR;
using OrderService.Infrastructure.Services;

namespace OrderService.API.Hubs
{
    public class TrackingHub : Hub
    {
        private readonly ICustomConnectionManager _connectionManager;

        public TrackingHub(ICustomConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public async Task SubscribeToOrder(string orderId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, orderId);
            _connectionManager.AddConnection(orderId, Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var orderId = _connectionManager.GetOrderId(Context.ConnectionId);
            if (orderId != null)
            {
                _connectionManager.RemoveConnection(orderId, Context.ConnectionId);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
