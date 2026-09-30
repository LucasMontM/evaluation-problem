using Ambev.DeveloperEvaluation.Domain.Services;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.Common;

/// <summary>
/// Validator for a sale product line
/// </summary>
public class SaleItemCommandValidator : AbstractValidator<SaleItemCommand>
{
    public SaleItemCommandValidator()
    {
        RuleFor(i => i.ProductId).NotEmpty();
        RuleFor(i => i.ProductName).NotEmpty().MaximumLength(200);
        RuleFor(i => i.Quantity).GreaterThan(0);
        RuleFor(i => i.Quantity)
            .LessThanOrEqualTo(QuantityDiscountPolicy.MaxQuantityPerProduct)
            .WithMessage($"It is not possible to sell more than {QuantityDiscountPolicy.MaxQuantityPerProduct} identical items.");
        RuleFor(i => i.UnitPrice).GreaterThan(0);
    }
}
