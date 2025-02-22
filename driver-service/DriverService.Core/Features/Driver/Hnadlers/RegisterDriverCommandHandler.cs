using DriverService.Core.Dtos;
using DriverService.Core.Features.Driver.Commands;
using DriverService.Domain.Entities;
using DriverService.Domain.Interfaces;
using MediatR;
namespace DriverService.Core.Features.Driver.Hnadlers
{
    public class RegisterDriverCommandHandler
            : IRequestHandler<RegisterDriverCommand, DriverResponse>
    {
        private readonly IDriverRepository _repository;

        public RegisterDriverCommandHandler(IDriverRepository repository)
        {
            _repository = repository;
        }

        public async Task<DriverResponse> Handle(
            RegisterDriverCommand request,
            CancellationToken cancellationToken)
        {
            var driver = new Domain.Entities.Driver
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                VehicleType = request.VehicleType,
                IsAvailable = true,
                RegistrationDate = DateTime.UtcNow
            };

            await _repository.AddAsync(driver);
            await _repository.SaveChangesAsync();

            return MapToDriverResponse(driver);
        }

        private static DriverResponse MapToDriverResponse(Domain.Entities.Driver driver)
        {
            return new DriverResponse(
                driver.Id,
                driver.Name,
                driver.VehicleType,
                driver.IsAvailable);
        }
    }
}