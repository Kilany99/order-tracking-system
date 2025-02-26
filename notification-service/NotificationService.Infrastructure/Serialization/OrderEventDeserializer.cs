using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace NotificationService.Infrastructure.Serialization;

public class OrderEventDeserializer : IDeserializer<OrderNotificationEvent>
{
    private readonly ILogger<OrderEventDeserializer> _logger;

    public OrderEventDeserializer(ILogger<OrderEventDeserializer> logger)
    {
        _logger = logger;
    }

    public OrderNotificationEvent Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull) return null;

        try
        {
            var jsonString = System.Text.Encoding.UTF8.GetString(data);
            _logger.LogInformation("Attempting to deserialize: {JsonString}", jsonString);

            var jsonDoc = JsonDocument.Parse(jsonString);
            var root = jsonDoc.RootElement;

            var orderEvent = new OrderNotificationEvent
            {
                OrderId = root.GetProperty("orderId").GetGuid(),
                Timestamp = DateTime.UtcNow
            };

            // Try to extract other properties
            if (root.TryGetProperty("customerId", out var customerId))
                orderEvent.CustomerId = customerId.GetString();

            if (root.TryGetProperty("customerEmail", out var customerEmail))
                orderEvent.CustomerEmail = customerEmail.GetString();

            if (root.TryGetProperty("customerName", out var customerName))
                orderEvent.CustomerName = customerName.GetString();

            if (root.TryGetProperty("driverId", out var driverId))
                orderEvent.DriverId = driverId.GetGuid();

            if (root.TryGetProperty("driverName", out var driverName))
                orderEvent.DriverName = driverName.GetString();

            if (root.TryGetProperty("deliveryLatitude", out var lat))
                orderEvent.DeliveryLatitude = lat.GetDouble();

            if (root.TryGetProperty("deliveryLongitude", out var lon))
                orderEvent.DeliveryLongitude = lon.GetDouble();

            // Determine status based on message structure
            orderEvent.Status = DetermineOrderStatus(root);

            _logger.LogInformation("Successfully deserialized to type OrderNotificationEvent");
            return orderEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize event");
            throw;
        }
    }

    private OrderStatus DetermineOrderStatus(JsonElement root)
    {
        if (root.TryGetProperty("createdAt", out _))
            return OrderStatus.Created;

        if (root.TryGetProperty("assignedAt", out _))
            return OrderStatus.Assigned;

        if (root.TryGetProperty("pickupTime", out _))
            return OrderStatus.OutForDelivery;

        if (root.TryGetProperty("deliveryTime", out _))
            return OrderStatus.Delivered;

        return OrderStatus.Created; // Default status
    }
}