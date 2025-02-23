import { OrderService } from './services/order-service.js';
import authService from './lib/auth-serivce.js';

const orderService = new OrderService(`https://localhost:7094`);

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
   
    document.getElementById('trackingForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        await handleTrackOrder();
    });
}
  // Show/hide loading indicator
  function showLoading() {
    document.getElementById('loading').style.display = 'block';
}

function hideLoading() {
    document.getElementById('loading').style.display = 'none';
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
    console.log('handleTrackOrder called');
    const orderIdInput = document.getElementById('orderId');
    const orderId = orderIdInput.value.trim();
    const trackBtn = document.getElementById('trackOrderBtn');
    const assignmentLoader = document.getElementById('assignmentLoader');

    // Hide all animations initially
    $('.lottie-animation').hide();
    $('#enjoyMealMessage').hide();

    if (!orderId) {
        showError("Please enter a valid Order ID");
        orderIdInput.focus();
        return;
    }

    try {
        trackBtn.disabled = true;
        const originalText = trackBtn.innerHTML;
        trackBtn.innerHTML = `
            <span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>
            Tracking...
        `;

        const order = await orderService.trackOrder(orderId);
        updateOrderDisplay(order);

        // Show assignment loader if order is in Created status and no driver assigned
        if (order.status === 0 && !order.driverId) {
            assignmentLoader.style.display = 'block';
            $('#assigningAnimation').show();
        }

        if (order.status !== 3) { // If not delivered
            orderService.initializeMap('map', [order.deliveryLatitude, order.deliveryLongitude]);
            orderService.addPackageMarker(
                [order.deliveryLatitude, order.deliveryLongitude],
                order.deliveryAddress
            );
        }

        await orderService.connectSignalR(orderId);

        // Status check interval
        const statusCheckInterval = setInterval(async () => {
            try {
                const updatedOrder = await orderService.trackOrder(orderId);
                updateOrderDisplay(updatedOrder);

                if (updatedOrder.status !== 0 || updatedOrder.driverId) {
                    assignmentLoader.style.display = 'none';
                }

                if (updatedOrder.status === 4) {
                    clearInterval(statusCheckInterval);
                    showError("Order was cancelled - No drivers available");
                }

                if (updatedOrder.status === 3) {
                    clearInterval(statusCheckInterval);
                }
            } catch (error) {
                console.error('Status check error:', error);
                clearInterval(statusCheckInterval);
            }
        }, 5000);

    } catch (error) {
        console.error('Error tracking order:', error);
        showError(error.message);
        assignmentLoader.style.display = 'none';
    } finally {
        trackBtn.disabled = false;
        trackBtn.innerHTML = 'Track Order';
    }
}
function updateOrderDisplay(order) {
    const statusMap = ["Created", "Preparing", "Out for Delivery", "Delivered", "Cancelled"];
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

    // Hide all animations first
    $('.lottie-animation').hide();
    $('#enjoyMealMessage').hide();
    $('#cancelMessage').hide();
    $('#map-container').show();

    // Show appropriate animation based on status
    switch(order.status) {
        case 0: // Created
            $('#createdAnimation').show();
            if (!order.driverId) {
                $('#assigningAnimation').show();
            }
            break;
        case 1: // Preparing
            $('#preparingAnimation').show();
            break;
        case 2: // Out for Delivery
            $('#deliveringAnimation').show();
            break;
        case 3: // Delivered
            $('#deliveredAnimation').show();
            $('#enjoyMealAnimation').show();
            $('#enjoyMealMessage').show();
            $('#map-container').hide(); // Hide map when delivered
            break;
        case 4: // Cancelled
            $('#cancelledAnimation').show();
            $('#cancelMessage').show();
            $('#map-container').hide(); // Hide map when cancelled
            break;
    }
}

function showError(message) {
    $('#errorMessage').text(message).fadeIn().delay(5000).fadeOut();
}

function showMessage(message) {
    $('#successMessage').text(message).fadeIn().delay(5000).fadeOut();
}