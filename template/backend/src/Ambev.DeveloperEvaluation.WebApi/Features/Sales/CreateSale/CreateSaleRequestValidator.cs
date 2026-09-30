using Ambev.DeveloperEvaluation.WebApi.Features.Sales.Common;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;

/// <summary>
/// Validator for CreateSaleRequest
/// </summary>
public class CreateSaleRequestValidator : AbstractValidator<CreateSaleRequest>
{
    public CreateSaleRequestValidator()
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
        RuleForEach(s => s.Items).SetValidator(new SaleItemRequestValidator());
    }
}
