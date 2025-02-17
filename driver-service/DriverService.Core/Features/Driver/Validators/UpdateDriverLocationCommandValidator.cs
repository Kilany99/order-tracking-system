
using DriverService.Core.Features.Driver.Commands;
using FluentValidation;

namespace DriverService.Core.Features.Driver.Validators
{
    public class UpdateDriverLocationCommandValidator : AbstractValidator<UpdateDriverLocationCommand>
    {
        public UpdateDriverLocationCommandValidator() 
        {
            RuleFor(x => x.DriverId).NotEmpty();
            RuleFor(x => x.Latitude).NotEmpty();
            RuleFor(x => x.Longitude).NotEmpty();
        }
    }
}
