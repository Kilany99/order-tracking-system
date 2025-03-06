using OrderService.Domain.Entities;
using OrderService.Domain.Models;

namespace OrderService.Domain.Extensions;


public static class OrderExtensions
{
    public static void EnrichWithDriverDetails(this Order order, DriverDetailsDto driver)
    {
        var driverDetails = new DriverDetails
        {
            Id = driver.Id,
            Name = driver.Name,
            PhoneNumber = driver.PhoneNumber,
            CurrentLatitude = driver.CurrentLatitude,
            CurrentLongitude = driver.CurrentLongitude
        };
        order.SetDriverDetails(driverDetails);
    }
}