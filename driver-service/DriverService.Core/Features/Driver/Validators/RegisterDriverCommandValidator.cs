using DriverService.Core.Features.Driver.Commands;
using FluentValidation;
namespace DriverService.Core.Features.Driver.Validators
{
    public class RegisterDriverCommandValidator : AbstractValidator<RegisterDriverCommand>
    { 
        public RegisterDriverCommandValidator() 
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
            RuleFor(x => x.VehicleType).NotEmpty().MaximumLength(50);
          
        }
    }
}
