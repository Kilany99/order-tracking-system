
namespace NotificationService.Application.Tests;


public class TestStartup
{
    public static void ConfigureTestEnvironment()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }
}