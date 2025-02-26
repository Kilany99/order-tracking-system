
using NotificationService.Domain.DTOs;

namespace NotificationService.Domain.Interfaces;


public interface ICustomerService
{
    Task<CustomerInfo> GetCustomerInfoAsync(string customerId);
}