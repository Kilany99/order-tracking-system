import authService  from '../lib/auth-serivce.js';

const DRIVER_ICON = L.icon({
  iconUrl: 'https://cdn-icons-png.flaticon.com/512/4474/4474288.png',
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
  }

  async placeOrder(orderData) {
    try {
      const response = await $.ajax({
        url: `${this.baseUrl}/api/orders`,
        type: 'POST',
        contentType: 'application/json',
        headers: { 'Authorization': `Bearer ${authService.token}` },
        data: JSON.stringify(orderData)
      });
      return response;
    } catch (error) {
      this.handleError(error, 'Failed to place order');
    }
  }

  async trackOrder(orderId) {
    try {
      const response = await $.ajax({
        url: `${this.baseUrl}/api/orders/${orderId}`,
        type: 'GET',
        headers: { 'Authorization': `Bearer ${authService.token}` }
      });
      return response;
    } catch (error) {
      this.handleError(error, 'Failed to track order');
    }
  }

  initializeMap(containerId, coords) {
    if (this.map) this.map.remove();
    
    this.map = L.map(containerId).setView(coords, 13);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png').addTo(this.map);
    
    // this.markers.order = L.marker(coords, { icon: PACKAGE_ICON })
    //   .addTo(this.map)
    //   .bindPopup(`
    //     <b>Delivery Location</b><br>
    // `).openPopup();
    }
    addPackageMarker(coords, address) {
      if (this.markers.order) {
        this.map.removeLayer(this.markers.order);
      }
      
      this.markers.order = L.marker(coords, { icon: PACKAGE_ICON })
        .addTo(this.map)
        .bindPopup(`
          <b>Delivery Location</b><br>
          ${address}
        `).openPopup();
    }
  updateDriverPosition(lat, lng) {
    if (!this.markers.driver) {
      this.markers.driver = L.marker([lat, lng], { icon: DRIVER_ICON })
        .addTo(this.map)
        .bindPopup('Driver Location');
    } else {
      this.markers.driver.setLatLng([lat, lng]);
    }
    this.map.panTo([lat, lng], { animate: true, duration: 0.5 });
  }

  async connectSignalR(orderId) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.baseUrl}/tracking`, {
        accessTokenFactory: () => authService.token
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on("DriverLocationUpdate", ({ lat, lng }) => {
      this.updateDriverPosition(lat, lng);
    });

    try {
      await this.connection.start();
      await this.connection.invoke("SubscribeToOrder", orderId);
    } catch (err) {
      console.error('SignalR connection error:', err);
      setTimeout(() => this.connectSignalR(orderId), 5000);
    }
  }

  handleError(error, defaultMessage) {
    const message = error.responseJSON?.message || defaultMessage;
    throw new Error(message);
  }
}