
namespace NotificationService.Domain.Templates;


public static class OrderEmailTemplates
{
    public static string OrderCreated(string customerName, string orderId, decimal totalAmount)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'>
                <h2 style='color: #2c3e50;'>Order Confirmation</h2>
                <p>Dear {customerName},</p>
                <p>Thank you for your order! We've received your order and it's being processed.</p>
                <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px;'>
                    <p><strong>Order Details:</strong></p>
                    <p>Order ID: #{orderId}</p>
                    <p>Total Amount: ${totalAmount}</p>
                </div>
                <p>We'll keep you updated on your order status.</p>
                <p>Best regards,<br>Your Delivery Team</p>
            </div>
        </body>
        </html>";
    }

    public static string OrderAssigned(string customerName, string orderId, string driverName)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'>
                <h2 style='color: #2c3e50;'>Driver Assigned to Your Order</h2>
                <p>Dear {customerName},</p>
                <p>Good news! A driver has been assigned to your order.</p>
                <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px;'>
                    <p><strong>Order Details:</strong></p>
                    <p>Order ID: #{orderId}</p>
                    <p>Driver Name: {driverName}</p>
                </div>
                <p>Your order will be prepared soon!</p>
                <p>Best regards,<br>Your Delivery Team</p>
            </div>
        </body>
        </html>";
    }

    public static string OrderPreparing(string customerName, string orderId)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'>
                <h2 style='color: #2c3e50;'>Your Order is Being Prepared</h2>
                <p>Dear {customerName},</p>
                <p>We're currently preparing your order #{orderId}.</p>
                <p>We'll notify you once it's ready for delivery!</p>
                <p>Best regards,<br>Your Delivery Team</p>
            </div>
        </body>
        </html>";
    }

    public static string OrderOutForDelivery(string customerName, string orderId, string driverName)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'>
                <h2 style='color: #2c3e50;'>Your Order is Out for Delivery!</h2>
                <p>Dear {customerName},</p>
                <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px;'>
                    <p>Your order #{orderId} is now on its way to you.</p>
                    <p>Driver Name: {driverName}</p>
                </div>
                <p>You can track your order in real-time through our website.</p>
                <p>Best regards,<br>Your Delivery Team</p>
            </div>
        </body>
        </html>";
    }

    public static string OrderDelivered(string customerName, string orderId)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'>
                <h2 style='color: #2c3e50;'>Order Delivered Successfully!</h2>
                <p>Dear {customerName},</p>
                <p>Your order #{orderId} has been delivered successfully.</p>
                <p>We hope you enjoy your order! Thank you for choosing our service.</p>
                <p>If you have a moment, we'd love to hear your feedback about our service.</p>
                <p>Best regards,<br>Your Delivery Team</p>
            </div>
        </body>
        </html>";
    }

    public static string OrderCancelled(string customerName, string orderId)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'>
                <h2 style='color: #2c3e50;'>Order Cancellation Notice</h2>
                <p>Dear {customerName},</p>
                <p>We regret to inform you that your order #{orderId} has been cancelled.</p>
                <p>If you believe this is an error, please contact our customer support.</p>
                <p>We apologize for any inconvenience caused.</p>
                <p>Best regards,<br>Your Delivery Team</p>
            </div>
        </body>
        </html>";
    }
}