using FluentValidation;
using OrderService.Application.Features.Orders.Commands;

namespace OrderService.Application.Validators;

public class DeleteOrderCommandValidator : AbstractValidator<DeleteOrderCommand>
{
    public DeleteOrderCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Order ID is required.");
    }
}
