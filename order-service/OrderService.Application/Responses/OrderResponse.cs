using OrderService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Application.Responses;


public class OrderResponse
{
    public Guid Id { get; set; }
    public string CustomerId { get; set; }
    public string DeliveryAddress { get; set; }
    public OrderStatus Status {  get; set; }
    public double DeliveryLatitude { get; set; }
    public double DeliveryLongitude { get; set; }

    public OrderResponse(Guid id,string cutomerId,string delvAddress,OrderStatus orderStatus, double latitude,double longitude) 
    {
        Id = id;
        CustomerId = cutomerId;
        DeliveryAddress = delvAddress;
        DeliveryLatitude = latitude;
        DeliveryLongitude = longitude;
        Status = orderStatus;
    }
}
