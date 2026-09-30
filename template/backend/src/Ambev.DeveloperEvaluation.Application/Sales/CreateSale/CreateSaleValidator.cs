using Ambev.DeveloperEvaluation.Application.Sales.Common;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

/// <summary>
/// Validator for CreateSaleCommand
/// </summary>
public class CreateSaleCommandValidator : AbstractValidator<CreateSaleCommand>
{
    public CreateSaleCommandValidator()
    {
        RuleFor(s => s.SaleNumber).NotEmpty().MaximumLength(50);
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
