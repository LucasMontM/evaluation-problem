using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

/// <summary>
/// Validator for ListSalesCommand
/// </summary>
public class ListSalesValidator : AbstractValidator<ListSalesCommand>
{
    public ListSalesValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Size).InclusiveBetween(1, 100);
        RuleFor(x => x.MaxSaleDate)
            .GreaterThanOrEqualTo(x => x.MinSaleDate)
            .When(x => x.MinSaleDate.HasValue && x.MaxSaleDate.HasValue);
        RuleFor(x => x.MaxTotalAmount)
            .GreaterThanOrEqualTo(x => x.MinTotalAmount)
            .When(x => x.MinTotalAmount.HasValue && x.MaxTotalAmount.HasValue);
    }
}
