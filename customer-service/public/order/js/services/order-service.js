import authService  from '../lib/auth-serivce.js';

const DRIVER_ICON = L.icon({
  iconUrl: 'https://img.icons8.com/?size=100&id=20XFfv36rpCn&format=png&color=000000',
  iconSize: [40, 40],
  iconAnchor: [20, 40],
  popupAnchor: [0, -40]
});

const PACKAGE_ICON = L.icon({
  iconUrl: 'https://cdn-icons-png.flaticon.com/512/684/684908.png',
  iconSize: [32, 32],
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


  // New method to handle driver location updates
  handleDriverLocationUpdate(lat, lng) {
      console.log("Handling driver location update:", lat, lng);
      
      if (!this.map) {
          console.error("Map not initialized");
          return;
      }

      // Update driver marker
      if (!this.markers.driver) {
          console.log("Creating new driver marker");
          this.markers.driver = L.marker([lat, lng], { 
              icon: DRIVER_ICON,
              zIndexOffset: 1000
          })
          .addTo(this.map)
          .bindPopup('Driver Location');
      } else {
          console.log("Updating existing driver marker");
          this.markers.driver.setLatLng([lat, lng]);
      }

      // Calculate and display route if we have both markers
      if (this.markers.order) {
          console.log("Updating route...");
          const deliveryPos = this.markers.order.getLatLng();
          this.updateRouteAndETA(
              lat, 
              lng, 
              deliveryPos.lat, 
              deliveryPos.lng
          ).catch(err => console.error("Error updating route:", err));
      }

      // Update map view
      if (this.markers.order) {
          const bounds = L.latLngBounds([
              [lat, lng],
              this.markers.order.getLatLng()
          ]);
          this.map.fitBounds(bounds, { padding: [50, 50] });
      } else {
          this.map.panTo([lat, lng], { animate: true, duration: 0.5 });
      }
  }

  async updateRouteAndETA(driverLat, driverLng, deliveryLat, deliveryLng) {
      console.log("Calculating route between:", 
          { driverLat, driverLng }, 
          { deliveryLat, deliveryLng }
      );

      try {
          const response = await fetch(
              `${this.baseUrl}/api/routing/route?` + 
              `startLat=${driverLat}&startLng=${driverLng}` +
              `&endLat=${deliveryLat}&endLng=${deliveryLng}`,
              {
                  headers: {
                      'Authorization': `Bearer ${authService.token}`,
                      'Accept': 'application/json'
                  }
              }
          );

          if (!response.ok) {
              const errorText = await response.text();
              console.error("Route API error:", errorText);
              throw new Error('Failed to get route');
          }

          const routeData = await response.json();
          console.log("Received route data:", routeData);
          
          this.displayRoute(routeData);
          
          // Update route info panel
          const distance = (routeData.distance / 1000).toFixed(1);
          const eta = new Date(routeData.estimatedArrival).toLocaleTimeString();
          this.updateRouteInfoPanel(distance, eta);
      } catch (error) {
          console.error('Error updating route:', error);
          this.displayDirectRoute(driverLat, driverLng, deliveryLat, deliveryLng);
      }
  }

  displayRoute(routeData) {
      console.log("Displaying route:", routeData);

      // Remove existing route and ETA marker
      if (this.routeLine) {
          this.map.removeLayer(this.routeLine);
      }
      if (this.etaMarker) {
          this.map.removeLayer(this.etaMarker);
      }

      // Create route line from route points
      const routePoints = routeData.routePoints.map(point => 
          [point.latitude, point.longitude]
      );

      this.routeLine = L.polyline(routePoints, {
          color: '#2196F3',
          weight: 4,
          opacity: 0.8,
          lineCap: 'round'
      }).addTo(this.map);

      // Calculate middle point for ETA display
      const middlePoint = routePoints[Math.floor(routePoints.length / 2)];

      // Format distance and ETA
      const distance = (routeData.distance / 1000).toFixed(1);
      const eta = new Date(routeData.estimatedArrival).toLocaleTimeString();

      // Create ETA marker
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

      // Show route info panel
      this.updateRouteInfoPanel(distance, eta);

      // Fit map bounds to show entire route
      this.map.fitBounds(this.routeLine.getBounds(), { padding: [50, 50] });
  }

  updateRouteInfoPanel(distance, eta) {
      console.log("Updating route info panel:", { distance, eta });
      const routeInfo = document.getElementById('routeInfo');
      if (routeInfo) {
          document.getElementById('distance').textContent = `Distance: ${distance} km`;
          document.getElementById('eta').textContent = `ETA: ${eta}`;
          document.getElementById('trafficInfo').textContent = 
              `Traffic: ${this.getTrafficCondition()}`;
          routeInfo.style.display = 'block';
      }
  }


  getTrafficCondition() {
      const hour = new Date().getHours();
      if ((hour >= 7 && hour <= 9) || (hour >= 16 && hour <= 18)) {
          return 'Heavy (Peak Hours)';
      }
      return 'Normal';
  }

 


  async trackOrder(orderId) {
      try {
          const response = await fetch(`${this.baseUrl}/api/orders/${orderId}`, {
              method: 'GET',
              headers: {
                  'Authorization': `Bearer ${authService.token}`,
                  'Content-Type': 'application/json'
              }
          });

          if (!response.ok) {
              const error = await response.json();
              throw new Error(error.message || 'Failed to track order');
          }

          return await response.json();
      } catch (error) {
          console.error('Error tracking order:', error);
          throw new Error(error.message || 'Failed to track order');
      }
  }

  async placeOrder(orderData) {
      try {
          const response = await fetch(`${this.baseUrl}/api/orders`, {
              method: 'POST',
              headers: {
                  'Authorization': `Bearer ${authService.token}`,
                  'Content-Type': 'application/json'
              },
              body: JSON.stringify(orderData)
          });

          if (!response.ok) {
              const error = await response.json();
              throw new Error(error.message || 'Failed to place order');
          }

          return await response.json();
      } catch (error) {
          console.error('Error placing order:', error);
          throw new Error(error.message || 'Failed to place order');
      }
  }

  initializeMap(containerId, coords) {
      if (this.map) {
          this.map.remove();
          this.markers = {
              order: null,
              driver: null
          };
      }
      
      this.map = L.map(containerId).setView(coords, 13);
      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png').addTo(this.map);
  }

  addPackageMarker(coords, address) {
      if (this.markers.order) {
          this.map.removeLayer(this.markers.order);
      }
      
      this.markers.order = L.marker(coords, { 
          icon: PACKAGE_ICON,
          zIndexOffset: 100
      })
      .addTo(this.map)
      .bindPopup(`
          <b>Delivery Location</b><br>
          ${address}
      `).openPopup();

      // Center map to show the delivery location
      this.map.setView(coords, 13);
  }

  updateDriverPosition(lat, lng) {
      console.log("Updating driver position:", lat, lng);
      
      if (!this.map) {
          console.error("Map not initialized");
          return;
      }

      if (!this.markers.driver) {
          console.log("Creating new driver marker");
          this.markers.driver = L.marker([lat, lng], { 
              icon: DRIVER_ICON,
              zIndexOffset: 1000
          })
          .addTo(this.map)
          .bindPopup('Driver Location');
      } else {
          console.log("Updating existing driver marker");
          this.markers.driver.setLatLng([lat, lng]);
      }

      // Adjust map view to show both driver and delivery location
      if (this.markers.order) {
          const bounds = L.latLngBounds([
              [lat, lng],
              this.markers.order.getLatLng()
          ]);
          this.map.fitBounds(bounds, { padding: [50, 50] });
      } else {
          this.map.panTo([lat, lng], { animate: true, duration: 0.5 });
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
            .withAutomaticReconnect()
            .build();

        this.connection.on("DriverLocationUpdate", (data) => {
            console.log("Received driver location update:", data);
            if (data.lat && data.lng) {
                this.handleDriverLocationUpdate(data.lat, data.lng);  // Use this method instead
            }
        });

        await this.connection.start();
        console.log("SignalR Connected");

        await this.connection.invoke("SubscribeToOrder", orderId);
        console.log("Subscribed to order:", orderId);
    } catch (err) {
        console.error('SignalR connection error:', err);
        setTimeout(() => this.connectSignalR(orderId), 5000);
    }
}
}

