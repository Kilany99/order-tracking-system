namespace NotificationService.Models;

public class NotificationEvent
{
    public string EventType { get; set; }
    public string Topic { get; set; }
    public Dictionary<string, string> Data { get; set; }
}