using Microsoft.AspNetCore.SignalR;
using OrderService.API.Clients;
using OrderService.Infrastructure.Services;

namespace OrderService.API.Hubs
{
    public class TrackingHub : Hub
    {
        private readonly ICustomConnectionManager _connectionManager;
        private readonly IDriverClient _driverLocationService;
        private readonly ILogger<TrackingHub> _logger;

        public TrackingHub(ICustomConnectionManager connectionManager, IDriverClient driverLocationService, ILogger<TrackingHub> logger)
        {
            _connectionManager = connectionManager;
            _driverLocationService = driverLocationService;
            _logger = logger;

        }

      
            public async Task SubscribeToOrder(Guid orderId)
            {
            try
            {
                _logger.LogInformation("--//TrackingHub//--:Client subscribing to order {OrderId}", orderId);

                await Groups.AddToGroupAsync(Context.ConnectionId, orderId.ToString());
                _connectionManager.AddConnection(orderId.ToString(), Context.ConnectionId);

                // Send initial driver location if available
                var location = await _driverLocationService.GetDriverLocation(orderId);
                if (location != null)
                {
                    _logger.LogInformation("Sending initial driver location for order {OrderId}", orderId);
                    await Clients.Caller.SendAsync("DriverLocationUpdate", new
                    {
                        orderId,
                        driverId = location.DriverId,
                        lat = location.Latitude,
                        lng = location.Longitude,
                        timestamp = location.Timestamp
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "--//TrackingHub//--:Error in SubscribeToOrder for {OrderId}", orderId);
                throw;
            }
        }

        
        public async Task UpdateDriverPosition(double lat, double lng)
        {
            var driverId = Context.User?.FindFirst("driverId")?.Value;
            if (!string.IsNullOrEmpty(driverId))
            {
                await _driverLocationService.UpdateDriverPosition(Guid.Parse(driverId), lat, lng);
                await Clients.Group(driverId).SendAsync("PositionUpdated", lat, lng);
            }
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
