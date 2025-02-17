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
            
            if (!this.driverMarker) {
                this.driverMarker = L.marker(latLng).addTo(map);
                map.setView(latLng, 13);
            } else {
                this.driverMarker.setLatLng(latLng);
                // Optional: Smooth transition
                // map.panTo(latLng, {animate: true, duration: 0.5});
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

// Initialize order service
const orderServiceInstance = new OrderService("http://localhost:5000", authService);
window.orderServiceInstance = orderServiceInstance;

// UI Initialization
function initializeOrderUI(orderService, map) {
    // Show/hide UI elements based on auth
    if (orderService.authService.isAuthenticated()) {
        $('#orderSection').show();
    }

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
            
            // Update status display
            updateStatusDisplay(order);
            
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
                <p>Last Updated: ${new Date(order.lastUpdated).toLocaleString()}</p>
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