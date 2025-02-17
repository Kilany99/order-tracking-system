
namespace DriverService.Domain.Exceptions;


public class DriverDomainException : Exception
{
    public DriverDomainException(string message) : base(message) { }
}