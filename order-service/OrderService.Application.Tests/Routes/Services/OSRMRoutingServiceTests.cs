using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using OrderService.Infrastructure.Routing.Models;
using OrderService.Infrastructure.Routing.Services;
using RichardSzalay.MockHttp;
using System.Net;


namespace OrderService.Infrastructure.Tests.Routing.Services
{
    public class OSRMRoutingServiceTests
    {
        private readonly MockHttpMessageHandler _mockHttp;
        private readonly HttpClient _httpClient;
        private readonly MemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly Mock<ILogger<OSRMRoutingService>> _logger;
        private readonly Mock<ITimeProvider> _timeProvider;
        private readonly OSRMRoutingService _service;

        public OSRMRoutingServiceTests()
        {
            _mockHttp = new MockHttpMessageHandler();
            _httpClient = _mockHttp.ToHttpClient();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { { "OSRM:BaseUrl", "http://router.project-osrm.org" } })
                .Build();
            _logger = new Mock<ILogger<OSRMRoutingService>>();
            _timeProvider = new Mock<ITimeProvider>();
            _service = new OSRMRoutingService(_httpClient, _logger.Object, _configuration, _cache, _timeProvider.Object);
        }

        [Fact]
        public async Task GetRoute_ShouldReturnRouteFromApi_WhenNotInCache()
        {
            // Arrange
            _mockHttp.When("http://router.project-osrm.org/route/v1/driving/2.0,1.0;4.0,3.0?overview=full&geometries=geojson&steps=true")
                     .Respond("application/json", JsonConvert.SerializeObject(new OSRMResponse
                     {
                         Routes = new List<OSRMRoute>
                         {
                             new OSRMRoute
                             {
                                 Distance = 1000,
                                 Duration = 600,
                                 Geometry = new Geometry { Coordinates = new List<List<double>> { new List<double> { 2.0, 1.0 }, new List<double> { 4.0, 3.0 } } }
                             }
                         }
                     }));

            // Act
            var route = await _service.GetRoute(1.0, 2.0, 3.0, 4.0);

            // Assert
            Assert.NotNull(route);
            Assert.Equal(1000, route.Distance);
            Assert.Equal(600, route.Duration);
            Assert.Equal(2, route.RoutePoints.Count);
            Assert.Equal(1.0, route.RoutePoints[0].Latitude);
            Assert.Equal(2.0, route.RoutePoints[0].Longitude);
            Assert.Equal(3.0, route.RoutePoints[1].Latitude);
            Assert.Equal(4.0, route.RoutePoints[1].Longitude);
        }

        [Fact]
        public async Task GetRoute_ShouldReturnCachedRoute_WhenInCache()
        {
            // Arrange
            var cachedRoute = new RouteResponse { Distance = 500, Duration = 300, RoutePoints = new List<Coordinate>() };
            _cache.Set("route_1.0_2.0_3.0_4.0", cachedRoute);
            var request = _mockHttp.When("http://router.project-osrm.org/route/v1/driving/2.0,1.0;4.0,3.0?overview=full&geometries=geojson&steps=true")
                                   .Respond("application/json", JsonConvert.SerializeObject(new OSRMResponse
                                   {
                                       Routes = new List<OSRMRoute> { new OSRMRoute { Distance = 1000, Duration = 600 } }
                                   }));

            // Act
            var route = await _service.GetRoute(1.0, 2.0, 3.0, 4.0);

            // Assert
            Assert.Equal(cachedRoute, route);
            Assert.Equal(0, _mockHttp.GetMatchCount(request)); // No HTTP call made
        }

        [Fact]
        public async Task GetRoute_ShouldThrowException_WhenNoRoutesFound()
        {
            // Arrange
            _mockHttp.When("http://router.project-osrm.org/route/v1/driving/2.0,1.0;4.0,3.0?overview=full&geometries=geojson&steps=true")
                     .Respond("application/json", JsonConvert.SerializeObject(new OSRMResponse { Routes = new List<OSRMRoute>() }));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _service.GetRoute(1.0, 2.0, 3.0, 4.0));
            Assert.Equal("No route found", exception.Message);
            _logger.Verify(l => l.LogError(It.IsAny<Exception>(), "Error getting route"), Times.Once());
        }

        [Fact]
        public async Task GetRoute_ShouldThrowHttpRequestException_WhenApiFails()
        {
            // Arrange
            _mockHttp.When("http://router.project-osrm.org/route/v1/driving/2.0,1.0;4.0,3.0?overview=full&geometries=geojson&steps=true")
                     .Respond(HttpStatusCode.InternalServerError);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _service.GetRoute(1.0, 2.0, 3.0, 4.0));
            _logger.Verify(l => l.LogError(It.IsAny<HttpRequestException>(), "Error getting route"), Times.Once());
        }

        [Fact]
        public async Task CalculateETA_ShouldApplyTrafficFactor_DuringPeakHours()
        {
            // Arrange
            _mockHttp.When("http://router.project-osrm.org/route/v1/driving/2.0,1.0;4.0,3.0?overview=full&geometries=geojson&steps=true")
                     .Respond("application/json", JsonConvert.SerializeObject(new OSRMResponse
                     {
                         Routes = new List<OSRMRoute> { new OSRMRoute { Duration = 600 } }
                     }));
            _timeProvider.Setup(tp => tp.Now).Returns(new DateTime(2023, 1, 1, 8, 0, 0)); // 8 AM, peak hour

            // Act
            var eta = await _service.CalculateETA(1.0, 2.0, 3.0, 4.0);

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(900), eta); // 600 * 1.5
        }

        [Fact]
        public async Task CalculateETA_ShouldApplyTrafficFactor_DuringNonPeakHours()
        {
            // Arrange
            _mockHttp.When("http://router.project-osrm.org/route/v1/driving/2.0,1.0;4.0,3.0?overview=full&geometries=geojson&steps=true")
                     .Respond("application/json", JsonConvert.SerializeObject(new OSRMResponse
                     {
                         Routes = new List<OSRMRoute> { new OSRMRoute { Duration = 600 } }
                     }));
            _timeProvider.Setup(tp => tp.Now).Returns(new DateTime(2023, 1, 1, 10, 0, 0)); // 10 AM, non-peak

            // Act
            var eta = await _service.CalculateETA(1.0, 2.0, 3.0, 4.0);

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(600), eta); // 600 * 1.0
        }

        [Fact]
        public async Task CalculateETA_ShouldPropagateException_WhenGetRouteFails()
        {
            // Arrange
            _mockHttp.When("http://router.project-osrm.org/route/v1/driving/2.0,1.0;4.0,3.0?overview=full&geometries=geojson&steps=true")
                     .Respond(HttpStatusCode.InternalServerError);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _service.CalculateETA(1.0, 2.0, 3.0, 4.0));
            _logger.Verify(l => l.LogError(It.IsAny<HttpRequestException>(), "Error calculating ETA"), Times.Once());
        }

        [Fact]
        public async Task CalculateDistance_ShouldReturnDistanceFromRoute()
        {
            // Arrange
            _mockHttp.When("http://router.project-osrm.org/route/v1/driving/2.0,1.0;4.0,3.0?overview=full&geometries=geojson&steps=true")
                     .Respond("application/json", JsonConvert.SerializeObject(new OSRMResponse
                     {
                         Routes = new List<OSRMRoute> { new OSRMRoute { Distance = 1000 } }
                     }));

            // Act
            var distance = await _service.CalculateDistance(1.0, 2.0, 3.0, 4.0);

            // Assert
            Assert.Equal(1000, distance);
        }

        [Fact]
        public async Task CalculateDistance_ShouldPropagateException_WhenGetRouteFails()
        {
            // Arrange
            _mockHttp.When("http://router.project-osrm.org/route/v1/driving/2.0,1.0;4.0,3.0?overview=full&geometries=geojson&steps=true")
                     .Respond(HttpStatusCode.InternalServerError);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _service.CalculateDistance(1.0, 2.0, 3.0, 4.0));
        }
    }
}