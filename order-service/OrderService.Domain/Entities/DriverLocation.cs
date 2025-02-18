namespace OrderService.Domain.Entities
{
	public class DriverLocation
	{
		public Guid DriverId { get; set; }
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public DateTime Timestamp { get; set; }
	}
}