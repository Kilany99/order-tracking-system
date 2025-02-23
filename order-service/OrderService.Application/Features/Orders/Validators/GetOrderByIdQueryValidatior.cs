using FluentValidation;
using OrderService.Application.Features.Orders.Queries;
using System.Data;

namespace OrderService.Application.Features.Orders.Validators;

public class GetOrderByIdQueryValidatior : AbstractValidator<GetOrderByIdQuery>
{
    public GetOrderByIdQueryValidatior()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("OrderId is required");
    }
}
