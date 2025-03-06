// public/js/order-history.js
import { OrderService } from './services/order-service.js';
import authService from '../js/lib/auth-serivce.js';

class OrderHistory {
    constructor() {
        this.orderService = new OrderService('https://localhost:7094');
        this.refreshInterval = null;
        this.statusMap = ["Created", "Preparing", "Out for Delivery", "Delivered", "Cancelled"];
        this.statusColors = {
            'Created': 'info',
            'Preparing': 'warning',
            'Out for Delivery': 'primary',
            'Delivered': 'success',
            'Cancelled': 'danger'
        };
        this.initialize();
    }

    async initialize() {
        if (!authService.isAuthenticated()) {
            window.location.href = '/auth/login.html';
            return;
        }

        this.setupEventListeners();
        await this.loadOrders();
        this.startAutoRefresh();
    }

    setupEventListeners() {
        document.addEventListener('click', (e) => {
            if (e.target.classList.contains('track-order-btn')) {
                const orderId = e.target.dataset.orderId;
                window.location.href = `./order-track.html?orderId=${orderId}`;
            }
        });
    }

    startAutoRefresh() {
        this.refreshInterval = setInterval(() => {
            this.refreshOrders();
        }, 30000); // Refresh every 30 seconds
    }

    stopAutoRefresh() {
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
            this.refreshInterval = null;
        }
    }

    async refreshOrders() {
        await this.loadOrders();
    }

    showLoading() {
        document.getElementById('loadingSpinner').classList.add('active');
    }

    hideLoading() {
        document.getElementById('loadingSpinner').classList.remove('active');
    }

    async loadOrders() {
        try {
            this.showLoading();
            const [activeOrders, allOrders] = await Promise.all([
                this.orderService.getActiveOrders(),
                this.orderService.getAllOrders()
            ]);

            this.displayActiveOrders(activeOrders);
            this.displayOrderHistory(allOrders);
        } catch (error) {
            this.showError('Failed to load orders');
            console.error('Error loading orders:', error);
        } finally {
            this.hideLoading();
        }
    }

    displayActiveOrders(orders) {
        const container = document.getElementById('activeOrders');
        if (!orders.length) {
            container.innerHTML = '<div class="col-12"><p class="text-muted">No active orders</p></div>';
            return;
        }

        container.innerHTML = orders.map(order => this.createActiveOrderCard(order)).join('');
    }

    createActiveOrderCard(order) {
        const status = this.statusMap[order.status]; // Convert numeric status to string
        return `
            <div class="col-md-4 mb-3">
                <div class="card order-card h-100">
                    <div class="card-body">
                        <h5 class="card-title">Order #${order.id.substring(0, 8)}</h5>
                        <p class="card-text">
                            <strong>Status:</strong> 
                            <span class="badge bg-${this.statusColors[status]}">
                                ${status}
                            </span><br>
                            <strong>Address:</strong> ${order.deliveryAddress}<br>
                            <strong>Date:</strong> ${new Date(order.createdAt).toLocaleString()}
                        </p>
                        <button class="btn btn-primary track-order-btn" 
                                data-order-id="${order.id}">
                            <i class="fas fa-map-marker-alt"></i> Track Order
                        </button>
                    </div>
                </div>
            </div>
        `;
    }

    displayOrderHistory(orders) {
        const tbody = document.getElementById('orderHistory');
        if (!orders.length) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="5" class="text-center text-muted">No order history</td>
                </tr>
            `;
            return;
        }

        tbody.innerHTML = orders.map(order => {
            const status = this.statusMap[order.status]; // Convert numeric status to string
            return `
                <tr>
                    <td>${order.id.substring(0, 8)}</td>
                    <td>${new Date(order.createdAt).toLocaleString()}</td>
                    <td>
                        <span class="badge bg-${this.statusColors[status]}">
                            ${status}
                        </span>
                    </td>
                    <td>${order.deliveryAddress}</td>
                    <td>
                        ${this.getActionButton(order)}
                    </td>
                </tr>
            `;
        }).join('');
    }

    getActionButton(order) {
        const status = this.statusMap[order.status]; // Convert numeric status to string
        const trackableStatuses = ['Created', 'Preparing', 'Out for Delivery'];
        return trackableStatuses.includes(status)
            ? `<button class="btn btn-sm btn-primary track-order-btn" 
                       data-order-id="${order.id}">
                   <i class="fas fa-map-marker-alt"></i> Track
               </button>`
            : '';
    }
w

    showError(message) {
        const toast = new bootstrap.Toast(document.getElementById('errorToast'));
        document.getElementById('errorToastMessage').textContent = message;
        toast.show();
    }
}

// Initialize on page load
const orderHistory = new OrderHistory();
window.orderHistory = orderHistory; // Make it accessible globally for the refresh button