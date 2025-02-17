namespace OrderService.Application.Responses;

public class ApiResponse<T>(T data, string message = "Success")
{
    public T Data { get; set; } = data;
    public string Message { get; set; } = message;
    public bool Success { get; set; } = true;
}
