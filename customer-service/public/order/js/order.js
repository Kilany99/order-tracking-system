class OrderService {
    constructor(baseUrl, authService) {
        this.baseUrl = baseUrl;
        this.authService = authService;
        this.connection = null;
        this.driverMarker = null;
    }

    async placeOrder(orderData) {
        return $.ajax({
            url: `${this.baseUrl}/api/orders`,
            type: 'POST',
            contentType: 'application/json',
            dataType: 'json',
            headers: { 'Authorization': `Bearer ${this.authService.token}` },
            data: JSON.stringify(orderData)
        });
    }

    async trackOrder(orderId) {
        return $.ajax({
            url: `${this.baseUrl}/api/orders/${orderId}`,
            type: 'GET',
            dataType: 'json',
            headers: { 'Authorization': `Bearer ${this.authService.token}` }
        });
    }

    initializeSignalR(map, orderId) {
        if (this.connection) {
            this.connection.stop();
        }

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(`${this.baseUrl}/tracking`, {
                accessTokenFactory: () => this.authService.token
            })
            .withAutomaticReconnect()
            .build();

            this.connection.on("locationUpdate", (location) => {
                const latLng = [location.latitude, location.longitude];
                
                // Create unique ID for each driver
                const driverId = location.driverId || 'default';
                
                if (!this.driverMarkers) this.driverMarkers = {};
                
                // Update existing marker or create new one
                if (this.driverMarkers[driverId]) {
                    this.driverMarkers[driverId].setLatLng(latLng);
                } else {
                    this.driverMarkers[driverId] = L.marker(latLng, { 
                        icon: driverIcon,
                        title: `Driver ${driverId}`
                    }).addTo(map);
                    
                    // Add popup with driver info
                    this.driverMarkers[driverId].bindPopup(`
                        <b>Driver ${driverId}</b><br>
                        Last update: ${new Date().toLocaleTimeString()}
                    `);
                }
                
                // Smooth transition to first driver
                if (Object.keys(this.driverMarkers).length === 1) {
                    map.setView(latLng, 13);
                } else {
                    map.panTo(latLng, { animate: true, duration: 0.5 });
                }
            });
        this.connection.onclose(async () => {
            console.log('Connection closed. Attempting to reconnect...');
            await this.connectToSignalR(orderId);
        });

        return this.connectToSignalR(orderId);
    }

    async connectToSignalR(orderId) {
        try {
            await this.connection.start();
            await this.connection.invoke("SubscribeToOrder", orderId);
        } catch (err) {
            console.error('SignalR connection error:', err);
            setTimeout(() => this.connectToSignalR(orderId), 5000);
        }
    }
}

// Auth service
const authService = {
    get token() {
        return localStorage.getItem("jwtToken");
    },
    isAuthenticated: () => !!localStorage.getItem("jwtToken")
};
const driverIcon = L.icon({
    iconUrl: 'https://cdn-icons-png.flaticon.com/512/4474/4474288.png', // Car icon
    iconSize: [38, 38], // size of the icon
    iconAnchor: [19, 38], // point of the icon which will correspond to marker's location
    popupAnchor: [0, -38] // point from which the popup should open relative to the iconAnchor
});

// Initialize order service
const orderServiceInstance = new OrderService("http://localhost:5000", authService);
window.orderServiceInstance = orderServiceInstance;

// UI Initialization
function initializeOrderUI(orderService, map) {
    // Show/hide UI elements based on auth
    if (orderService.authService.isAuthenticated()) {
        $('#orderSection').show();
    }// Submit order form
    $('#orderForm').submit(async e => {
        e.preventDefault();
        const orderData = {
            customerId: $('#customerId').val(),
            deliveryAddress: $('#deliveryAddress').val(),
            deliveryLatitude: parseFloat($('#deliveryLatitude').val()),
            deliveryLongitude: parseFloat($('#deliveryLongitude').val())
        };

        try {
            const response = await orderService.placeOrder(orderData);
            if (response && response.data) {
                showMessage(`Order placed! ID: ${response.data}`);
            } else {
                showMessage("Order placed successfully!");
            
            }
        } catch (error) {
            if (error.responseJSON && typeof error.responseJSON === 'object') {
                errorMessage = error.responseJSON.message ;
            } else {
                errorMessage = 'Failed to place order.';
            }
            showError(errorMessage);
        }
    });


    // Track order button handler
    $("#trackOrderBtn").click(async () => {
        const orderId = $("#orderId").val().trim();
        
        if (!orderId) {
            showError("Please enter a valid Order ID");
            return;
        }

        try {
            // Get order details
            const order = await orderService.trackOrder(orderId);
            
             // Clear existing delivery marker
            if (this.deliveryMarker) {
                map.removeLayer(this.deliveryMarker);
            }
            // Update status display
            updateStatusDisplay(order);
            // Add delivery location marker
            this.deliveryMarker = L.marker([order.deliveryLatitude, order.deliveryLongitude], {
                icon: L.icon({
                    iconUrl: 'https://cdn-icons-png.flaticon.com/512/4474/4474228.png', // Package icon
                    iconSize: [32, 32],
                    iconAnchor: [16, 32]
                })
            }).addTo(map);
            
            this.deliveryMarker.bindPopup(`
                <b>Delivery Location</b><br>
                ${order.deliveryAddress}
            `).openPopup();
            
            
            // Start SignalR connection
            await orderService.initializeSignalR(map, orderId);
            
            // Center map on delivery location
            map.setView([order.deliveryLatitude, order.deliveryLongitude], 13);

        } catch (error) {
            showError("Failed to track order. Please check the Order ID");
            console.error('Tracking error:', error);
        }
    });

    function updateStatusDisplay(order) {
        const statusMessages = {
            0: "Order Created",
            1: "Preparing",
            2: "Out for Delivery",
            3: "Delivered"
        };

        $('#orderStatus').html(`
            <div class="status-card">
                <h3>Order #${order.id}</h3>
                <p class="status-${order.status}">${statusMessages[order.status]}</p>
                <p>Delivery Address: ${order.deliveryAddress}</p>
                <p>Last Updated: ${new Date().toLocaleString()}</p>
            </div>
        `);
    }
}

// Helper functions
function showError(message) {
    $('#errorMessage').text(message).fadeIn().delay(5000).fadeOut();
}

function showMessage(message) {
    $('#successMessage').text(message).fadeIn().delay(5000).fadeOut();
}

// Expose initialization
window.initializeOrderUI = initializeOrderUI;