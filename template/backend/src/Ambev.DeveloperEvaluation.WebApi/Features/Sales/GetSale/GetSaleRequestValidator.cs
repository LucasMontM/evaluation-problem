using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.GetSale;

/// <summary>
/// Validator for GetSaleRequest
/// </summary>
public class GetSaleRequestValidator : AbstractValidator<GetSaleRequest>
{
    public GetSaleRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Sale ID is required");
    }
}
