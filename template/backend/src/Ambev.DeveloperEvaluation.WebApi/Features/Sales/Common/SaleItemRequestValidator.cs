using Ambev.DeveloperEvaluation.Domain.Services;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.Common;

/// <summary>
/// Validator for SaleItemRequest
/// </summary>
public class SaleItemRequestValidator : AbstractValidator<SaleItemRequest>
{
    public SaleItemRequestValidator()
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
