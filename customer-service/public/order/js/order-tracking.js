import { OrderService } from './services/order-service.js';
import authService from './lib/auth-serivce.js';

const orderService = new OrderService(`http://localhost:5000`);

$(document).ready(() => {
    initializeUI();
    setupEventHandlers();
});

function initializeUI() {
    if (authService.isAuthenticated()) {
        $('#orderSection').show();
    }
    initializeBaseMap();
}

function initializeBaseMap() {
    orderService.initializeMap('map', [37.7749, -122.4194]);
}

function setupEventHandlers() {
    $('#orderForm').submit(handleOrderSubmit);
    $('#trackOrderBtn').click(handleTrackOrder);
}

async function handleOrderSubmit(e) {
    e.preventDefault();
    const orderData = {
        customerId: $('#customerId').val(),
        deliveryAddress: $('#deliveryAddress').val(),
        deliveryLatitude: parseFloat($('#deliveryLatitude').val()),
        deliveryLongitude: parseFloat($('#deliveryLongitude').val())
    };

    try {
        await orderService.placeOrder(orderData);
        showMessage('Order placed successfully!');
    } catch (error) {
        showError(error.message);
    }
}

async function handleTrackOrder() {
    const orderId = $("#orderId").val().trim();
    if (!orderId) {
        showError("Please enter a valid Order ID");
        return;
    }

    try {
        const order = await orderService.trackOrder(orderId);
        updateOrderDisplay(order);
        
        orderService.initializeMap('map', [order.deliveryLatitude, order.deliveryLongitude]);
        orderService.addPackageMarker(
            [order.deliveryLatitude, order.deliveryLongitude],
            order.deliveryAddress
        );
        
        await orderService.connectSignalR(orderId);
    } catch (error) {
        showError(error.message);
    }
}

function updateOrderDisplay(order) {
    const statusMap = ["Created", "Preparing", "Out for Delivery", "Delivered"];
    const html = `
        <div class="status-card">
            <h3>Order #${order.id}</h3>
            <p class="status-${order.status}">${statusMap[order.status]}</p>
            <p>${order.deliveryAddress}</p>
            <p>Last Updated: ${new Date().toLocaleString()}</p>
            ${order.driverId ? `<p>Assigned Driver Id: #${order.driverId}</p>` : ''}
        </div>
    `;
    $('#orderStatus').html(html);
}

function showError(message) {
    $('#errorMessage').text(message).fadeIn().delay(5000).fadeOut();
}

function showMessage(message) {
    $('#successMessage').text(message).fadeIn().delay(5000).fadeOut();
}