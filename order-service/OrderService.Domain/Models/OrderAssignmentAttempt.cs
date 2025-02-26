namespace OrderService.Domain.Models;

public class OrderAssignmentAttempt
{
    public Guid OrderId { get; set; }
    public int RetryCount { get; set; }
    public DateTime LastAttemptTime { get; set; }
    public DateTime NextAttemptTime { get; set; }
    public bool IsCompleted { get; set; }
}
