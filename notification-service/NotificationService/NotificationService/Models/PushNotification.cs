namespace NotificationService.Models;



public class PushNotification
{
    public string UserId { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public Dictionary<string, string> Data { get; set; }
}