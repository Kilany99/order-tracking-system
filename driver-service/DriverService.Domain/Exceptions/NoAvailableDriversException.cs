
namespace DriverService.Domain.Exceptions;

public class NoAvailableDriversException : Exception
{
    public NoAvailableDriversException()
           : base($"No Available Driver Found!")
    {
    }

    public NoAvailableDriversException(string message) : base(message)
    {
    }
}
