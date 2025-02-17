
using Microsoft.AspNetCore.Identity;

namespace OrderService.Domain.Entities;

public class Customer : IdentityUser
{
    public string FullName { get; set; }
}