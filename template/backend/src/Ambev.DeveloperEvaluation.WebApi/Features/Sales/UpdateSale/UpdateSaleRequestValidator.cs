using Ambev.DeveloperEvaluation.WebApi.Features.Sales.Common;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;

/// <summary>
/// Validator for UpdateSaleRequest
/// </summary>
public class UpdateSaleRequestValidator : AbstractValidator<UpdateSaleRequest>
{
    public UpdateSaleRequestValidator()
    {
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
