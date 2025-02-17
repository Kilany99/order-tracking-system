using DriverService.Core.Features.Driver.Commands;
using FluentValidation;

namespace DriverService.Core.Features.Driver.Validators;

public class LoginDriverCommandValidator : AbstractValidator<LoginDriverCommand>
{
    public LoginDriverCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}
