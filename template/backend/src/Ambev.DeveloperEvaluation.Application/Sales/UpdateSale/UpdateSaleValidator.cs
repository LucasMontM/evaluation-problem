using Ambev.DeveloperEvaluation.Application.Sales.Common;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

/// <summary>
/// Validator for UpdateSaleCommand
/// </summary>
public class UpdateSaleCommandValidator : AbstractValidator<UpdateSaleCommand>
{
    public UpdateSaleCommandValidator()
    {
        RuleFor(s => s.Id).NotEmpty().WithMessage("Sale ID is required");
        RuleFor(s => s.SaleDate).NotEmpty();
        RuleFor(s => s.CustomerId).NotEmpty();
        RuleFor(s => s.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(s => s.BranchId).NotEmpty();
        RuleFor(s => s.BranchName).NotEmpty().MaximumLength(200);
        RuleFor(s => s.Items).NotEmpty().WithMessage("A sale must have at least one item.");
        RuleFor(s => s.Items)
            .Must(items => items.GroupBy(i => i.ProductId).All(g => g.Count() == 1))
            .WithMessage("Identical items must be informed in a single line.");
        RuleForEach(s => s.Items).SetValidator(new SaleItemCommandValidator());
    }
}
