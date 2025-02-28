using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Clients;
using OrderService.Infrastructure.Metrics;
using OrderService.Infrastructure.Services;

namespace OrderService.Infrastructure.Hubs
{
    public class TrackingHub : Hub
    {
        private readonly ICustomConnectionManager _connectionManager;
        private readonly IDriverClient _driverLocationService;
        private readonly ILogger<TrackingHub> _logger;
        private readonly OrderMetrics _metrics;

        public TrackingHub(ICustomConnectionManager connectionManager,
            IDriverClient driverLocationService, ILogger<TrackingHub> logger,
            OrderMetrics metrics)
        {
            _connectionManager = connectionManager;
            _driverLocationService = driverLocationService;
            _logger = logger;
            _metrics = metrics;

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
                    _logger.LogInformation("Sending initial driver location for order {OrderId}: Lat={Lat}, Lng={Lng}",
                                   orderId, location.Latitude, location.Longitude);
                    await Clients.Caller.SendAsync("DriverLocationUpdate", new
                    {
                        orderId,
                        driverId = location.DriverId,
                        lat = location.Latitude,
                        lng = location.Longitude,
                        timestamp = location.Timestamp
                    });
                }
                else
                {
                    _logger.LogInformation("No initial driver location available for order {OrderId}",
                        orderId);
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
            var startTime = DateTime.UtcNow;

            var driverId = Context.User?.FindFirst("driverId")?.Value;
            if (!string.IsNullOrEmpty(driverId))
            {
                await _driverLocationService.UpdateDriverPosition(Guid.Parse(driverId), lat, lng);
                await Clients.Group(driverId).SendAsync("PositionUpdated", lat, lng);
                var latency = (DateTime.UtcNow - startTime).TotalSeconds;
                _metrics.RecordLocationUpdate(latency);
            }
        }
        public async Task SendOrderStatusUpdate(Guid orderId, OrderStatus status, Guid? driverId = null)
        {
            await Clients.Group(orderId.ToString()).SendAsync("OrderStatusUpdated", new
            {
                orderId = orderId,
                status = (int)status,
                driverId = driverId,
                timestamp = DateTime.UtcNow
            });
        }

        public async Task BroadcastDriverLocation(Guid orderId, double lat, double lng)
        {
            try
            {
                _logger.LogInformation("Broadcasting driver location for order {OrderId}: Lat={Lat}, Lng={Lng}",
                    orderId, lat, lng);

                await Clients.Group(orderId.ToString()).SendAsync("DriverLocationUpdate", new
                {
                    orderId = orderId,
                    lat = lat,
                    lng = lng,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting driver location for order {OrderId}", orderId);
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
