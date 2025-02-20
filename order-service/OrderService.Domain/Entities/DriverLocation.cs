namespace OrderService.Domain.Entities
{
	public record DriverLocation
	(	Guid DriverId ,
		double Latitude ,
		double Longitude,
		DateTime Timestamp 
	);
}