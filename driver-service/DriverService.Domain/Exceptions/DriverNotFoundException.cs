
namespace DriverService.Domain.Exceptions
{
    public class DriverNotFoundException : Exception
    {
        public DriverNotFoundException(Guid driverId)
            : base($"Driver with ID {driverId} not found")
        {
        }

        public DriverNotFoundException(string message) : base(message)
        {
        }
    }
}
