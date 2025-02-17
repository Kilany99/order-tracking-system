
namespace DriverService.Domain.Entities;

public class DriverAuth
{
    public Guid DriverId { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public Driver Driver { get; set; }
}