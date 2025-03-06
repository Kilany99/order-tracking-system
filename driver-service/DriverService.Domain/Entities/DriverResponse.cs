
namespace DriverService.Domain.Entities;


public class DriverResponse
{
    public Guid Id { get; set; }
    public Guid? CurrentOrderId {  get; set; }
    public string Name { get; set; }
    public string VehicleType { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string PhoneNumber { get; set; }
    public DriverResponse() { }

    public DriverResponse(Guid id , string name, string vechicleType,bool isAvailable)
    {
        Id = id; Name = name; VehicleType = vechicleType; IsAvailable = isAvailable;
    }
    public void SetOrderId(Guid orderId)
        { CurrentOrderId = orderId; }
    public void SetPhoneNumber(string phoneNumber)
    {
        PhoneNumber = phoneNumber;
    }
}