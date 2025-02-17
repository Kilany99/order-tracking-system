

namespace DriverService.Domain.Exceptions;

public class DuplicateDriverException : Exception
{
    public DuplicateDriverException(string email)
        : base($"Driver with email {email} already exists") { }
}