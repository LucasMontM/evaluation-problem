using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSale;

/// <summary>
/// Validator for GetSaleCommand
/// </summary>
public class GetSaleValidator : AbstractValidator<GetSaleCommand>
{
    public GetSaleValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Sale ID is required");
    }
}
