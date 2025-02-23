import authService from '../lib/auth-serivce.js';

const DRIVER_ICON = L.divIcon({
  className: 'delivery-driver-icon',
  html: `
      <div class="delivery-animation" id="delivery-animation">
          <dotlottie-player
              src="https://lottie.host/de9c93b5-bb2d-4828-8090-4b457b35e24c/NgTsWX4Yc5.lottie"
              background="transparent"
              speed="1"
              style="width: 60px; height: 60px"
              loop
              autoplay
          ></dotlottie-player>
      </div>
  `,
  iconSize: [60, 60],
  iconAnchor: [30, 30],
  popupAnchor: [0, -30]
});
const PACKAGE_ICON = L.icon({
  iconUrl: 'https://cdn-icons-png.flaticon.com/512/684/684908.png',
  iconSize: [32, 32],
  iconAnchor: [16, 32],
  popupAnchor: [0, -32]
});

const PREPARING_ICON = L.icon({
  iconUrl: 'https://lottie.host/a6168637-8159-4796-8d3a-78cae59f3e51/CnXKAbAGiy.lottie',
  iconSize: [50, 50],
  iconAnchor: [16, 32],
  popupAnchor: [0, -32]
});

export class OrderService {
  constructor(baseUrl) {
    this.baseUrl = baseUrl;
    this.connection = null;
    this.map = null;
    this.markers = {
      order: null,
      driver: null
    };
    this.routeLine = null;
    this.etaMarker = null;
  }

  async _secureRequest(url, options = {}) {
    try {
      const response = await authService.secureFetch(url, {
        ...options,
        headers: {
          'Content-Type': 'application/json',
          ...options.headers
        }
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Request failed');
      }

      return await response.json();
    } catch (error) {
      console.error(`Request to ${url} failed:`, error);
      throw error;
    }
  }
  handleDriverLocationUpdate(lat, lng) {
    if (!this.map) {
        console.error("Map not initialized");
        return;
    }

    console.log("Updating driver location:", lat, lng);

    if (!this.markers.driver) {
        this.markers.driver = L.marker([lat, lng], { 
            icon: DRIVER_ICON,
            zIndexOffset: 1000
        }).addTo(this.map);
        this.lastPosition = { lat, lng };
    } else {
        const angle = this.calculateAngle(
            this.lastPosition.lat,
            this.lastPosition.lng,
            lat,
            lng
        );

        this.markers.driver.setLatLng([lat, lng]);

        // Rotate the entire animation container
        const animationContainer = this.markers.driver.getElement()
            .querySelector('.delivery-animation');
        if (animationContainer) {
            animationContainer.style.transform = `rotate(${angle}deg)`;
        }

        this.lastPosition = { lat, lng };
      }
    if (!this.markers.driver) {
        // Create new marker
        this.markers.driver = L.marker([lat, lng], { 
            icon: DRIVER_ICON,
            zIndexOffset: 1000
        }).addTo(this.map);
        console.log("Created new driver marker");
    } else {
        // Update existing marker
        this.markers.driver.setLatLng([lat, lng]);
        console.log("Updated driver marker position");
    }

    // Fit bounds if order marker exists
    if (this.markers.order) {
        const bounds = L.latLngBounds([
            [lat, lng],
            this.markers.order.getLatLng()
        ]);
        this.map.fitBounds(bounds, { padding: [50, 50] });
    } else {
        this.map.panTo([lat, lng], { animate: true, duration: 0.5 });
    }

    // Update route if order marker exists
    if (this.markers.order) {
        const deliveryPos = this.markers.order.getLatLng();
        this.updateRouteAndETA(lat, lng, deliveryPos.lat, deliveryPos.lng)
            .catch(err => console.error("Route update error:", err));
    }
}

async updateRouteAndETA(driverLat, driverLng, deliveryLat, deliveryLng) {
  try {
      const routeData = await this._secureRequest(
          `${this.baseUrl}/api/routing/route`,
          {
              method: 'POST',
              body: JSON.stringify({
                  startLat: driverLat,
                  startLng: driverLng,
                  endLat: deliveryLat,
                  endLng: deliveryLng
              })
          }
      );
    
      this.displayRoute(routeData);
      const distance = (routeData.distance / 1000).toFixed(1);
      const eta = new Date(routeData.estimatedArrival).toLocaleTimeString();
      this.updateRouteInfoPanel(distance, eta);
    
      return routeData;
  } catch (error) {
      console.error('Routing API error:', error);
      this.displayDirectRoute(driverLat, driverLng, deliveryLat, deliveryLng);
      throw error;
  }
}


  displayRoute(routeData) {
    if (this.routeLine) {
      this.map.removeLayer(this.routeLine);
    }
    if (this.etaMarker) {
      this.map.removeLayer(this.etaMarker);
    }

    const routePoints = routeData.routePoints.map(point => 
      [point.latitude, point.longitude]
    );

    this.routeLine = L.polyline(routePoints, {
      color: '#2196F3',
      weight: 4,
      opacity: 0.8,
      lineCap: 'round'
    }).addTo(this.map);

    const middlePoint = routePoints[Math.floor(routePoints.length / 2)];
    const distance = (routeData.distance / 1000).toFixed(1);
    const eta = new Date(routeData.estimatedArrival).toLocaleTimeString();

    this.etaMarker = L.marker(middlePoint, {
      icon: L.divIcon({
        className: 'eta-marker',
        html: `
          <div class="eta-label">
            <div>${distance} km</div>
            <div>ETA: ${eta}</div>
          </div>
        `,
        iconSize: [100, 40],
        iconAnchor: [50, 20]
      })
    }).addTo(this.map);

    this.updateRouteInfoPanel(distance, eta);
    this.map.fitBounds(this.routeLine.getBounds(), { padding: [50, 50] });
  }

  displayDirectRoute(startLat, startLng, endLat, endLng) {
    if (this.routeLine) {
      this.map.removeLayer(this.routeLine);
    }

    this.routeLine = L.polyline([[startLat, startLng], [endLat, endLng]], {
      color: '#ff0000',
      weight: 2,
      dashArray: '5,5',
      opacity: 0.7
    }).addTo(this.map);

    this.map.fitBounds(this.routeLine.getBounds(), { padding: [50, 50] });
  }

  updateRouteInfoPanel(distance, eta) {
    const routeInfo = document.getElementById('routeInfo');
    if (routeInfo) {
      document.getElementById('distance').textContent = `Distance: ${distance} km`;
      document.getElementById('eta').textContent = `ETA: ${eta}`;
      document.getElementById('trafficInfo').textContent = 
        `Traffic: ${this.getTrafficCondition()}`;
      routeInfo.style.display = 'block';
    }
  }
  calculateAngle(lat1, lng1, lat2, lng2) {
    const dx = lng2 - lng1;
    const dy = lat2 - lat1;
    let angle = Math.atan2(dy, dx) * 180 / Math.PI;
    
    // Adjust angle to point in the direction of movement
    angle = angle + 90; // Adjust based on your icon's default orientation
    
    return angle;
  }
  getTrafficCondition() {
    const hour = new Date().getHours();
    if ((hour >= 7 && hour <= 9) || (hour >= 16 && hour <= 18)) {
      return 'Heavy (Peak Hours)';
    }
    return 'Normal';
  }

  async trackOrder(orderId) {
    if (!orderId) {
      throw new Error('Order ID is required');
   }
   try {
        return this._secureRequest(
          `${this.baseUrl}/api/orders/${orderId}`,
          { method: 'GET' }
        );
   }catch (error) {
    throw new Error(`Failed to track order: ${error.message}`);
}
  }

  async placeOrder(orderData) {
    return this._secureRequest(
      `${this.baseUrl}/api/orders`,
      {
        method: 'POST',
        body: JSON.stringify(orderData)
      }
    );
  }

  initializeMap(containerId, coords) {
    if (this.map) {
      this.map.remove();
      this.markers = { order: null, driver: null };

    }
    
    this.map = L.map(containerId).setView(coords, 13);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap contributors'
    }).addTo(this.map);
  }
  // Add this method to your OrderService class
testDriverMarker() {
  if (!this.map) {
      console.error("Map not initialized");
      return;
  }


}

  addPackageMarker(coords, address) {
    if (this.markers.order) {
      this.map.removeLayer(this.markers.order);
    }
    
    this.markers.order = L.marker(coords, { 
      icon: PACKAGE_ICON,
      zIndexOffset: 100
    }).addTo(this.map)
      .bindPopup(`<b>Delivery Location</b><br>${address}`)
      .openPopup();

    this.map.setView(coords, 13);
  }

  updateDriverPosition(lat, lng) {
    if (!this.map) {
      console.error("Map not initialized");
      return;
    }

    if (!this.markers.driver) {
      this.markers.driver = L.marker([lat, lng], { 
        icon: DRIVER_ICON,
        zIndexOffset: 1000
      }).addTo(this.map).bindPopup('Driver Location');
    } else {
      this.markers.driver.setLatLng([lat, lng]);
    }

    if (this.markers.order) {
      const bounds = L.latLngBounds([
        [lat, lng],
        this.markers.order.getLatLng()
      ]);
      this.map.fitBounds(bounds, { padding: [50, 50] });
    }
  }

  async connectSignalR(orderId) {
    try {
      if (this.connection) {
        await this.connection.stop();
      }

      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(`${this.baseUrl}/tracking`, {
          accessTokenFactory: () => authService.token
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: retryContext => {
            if (retryContext.elapsedMilliseconds < 60000) {
              return 5000;
            }
            return null;
          }
        })
        .build();

      this.connection.on("DriverLocationUpdate", (data) => {
        if (data.lat && data.lng) {
          this.handleDriverLocationUpdate(data.lat, data.lng);
        }
      });
      this.connection.on("OrderStatusUpdated", (data) => {
        updateOrderDisplay(data);
        if (data.status !== 0) { // If status is not Created
            $("#assignmentLoader").hide();
        }
    });

      this.connection.onclose(async error => {
        if (error) {
          console.log('SignalR connection closed due to error:', error);
          setTimeout(() => this.connectSignalR(orderId), 5000);
        }
      });

      await this.connection.start();
      await this.connection.invoke("SubscribeToOrder", orderId);
    } catch (err) {
      console.error('SignalR connection error:', err);
      if (err.statusCode === 401) {
        authService.logout();
      }
      setTimeout(() => this.connectSignalR(orderId), 5000);
    }
  }
}