using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Domain.Entities;

public record DriverOrderResponse(
    Guid OrderId,
    string DeliveryAddress,
    double CurrentLat,
    double CurrentLon,
    string Status,
    DateTime? AssignedAt
);