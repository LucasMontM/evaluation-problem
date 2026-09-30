using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;

/// <summary>
/// Validator for DeleteSaleCommand
/// </summary>
public class DeleteSaleValidator : AbstractValidator<DeleteSaleCommand>
{
    public DeleteSaleValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Sale ID is required");
    }
}
