
namespace OrderService.Application.Responses;

public record CreateOrderRequest(string DeliveryAddress,double DeliveryLatitude, double DeliveryLongitude);
