// order.js - Handles order placement/tracking
class OrderService {
    constructor(baseUrl, authService) {
        this.baseUrl = baseUrl;
        this.authService = authService;
    }

    async placeOrder(orderData) {
        return $.ajax({
            url: `${this.baseUrl}/api/orders`,
            type: 'POST',
            contentType: 'application/json',
            headers: { 'Authorization': `Bearer ${this.authService.token}` },
            data: JSON.stringify(orderData)
        });
    }

    async trackOrder(orderId) {
        return $.ajax({
            url: `${this.baseUrl}/api/orders/${orderId}`,
            type: 'GET',
            headers: { 'Authorization': `Bearer ${this.authService.token}` }
        });
    }
}

// Order UI Handlers
function initializeOrderUI(orderService) {
    // Show order section if authenticated
    if (orderService.authService.isAuthenticated()) {
        $('#orderSection').show();
        $('#logout').show();
    }

    // Submit order form
    $('#orderForm').submit(async e => {
        e.preventDefault();
        const orderData = {
            customerId: $('#customerId').val(),
            deliveryAddress: $('#deliveryAddress').val(),
            deliveryLatitude: parseFloat($('#deliveryLatitude').val()),
            deliveryLongitude: parseFloat($('#deliveryLongitude').val())
        };

        try {
            const orderId = await orderService.placeOrder(orderData);
            alert(`Order placed! ID: ${orderId}`);
        } catch (error) {
            alert(`Error: ${error.responseJSON?.error || 'Failed to place order.'}`);
        }
    });

    // Track order button
    $('#trackOrderBtn').click(async () => {
        const orderId = $('#orderId').val();
        try {
            const order = await orderService.trackOrder(orderId);
            switch (order.status) {
                case 0:
                    statusMessage = "<p><strong>Status:</strong> Order has been created and is being processed.</p>";
                    break;
                case 1:
                    statusMessage = "<p><strong>Status:</strong> Order is being prepared.</p>";
                    break;
                case 2:
                    statusMessage = "<p><strong>Status:</strong> Order is out for delivery.</p>";
                    break;
                case 3:
                    statusMessage = "<p><strong>Status:</strong> Order has been delivered.</p>";
                    break;
                default:
                    statusMessage = "<p><strong>Status:</strong> Unknown status.</p>";
                    break;
            }

            $('#orderStatus').html(`
                ${statusMessage}
                <p><strong>Address:</strong> ${order.deliveryAddress}</p>
                <p><strong>Last Updated:</strong> ${new Date().toLocaleString()}</p>
            `);
        } catch (error) {
            $('#orderStatus').html('<p>Order not found.</p>');
        }
    });
}