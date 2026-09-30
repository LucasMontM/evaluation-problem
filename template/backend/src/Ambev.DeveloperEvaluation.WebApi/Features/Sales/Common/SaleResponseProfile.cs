using Ambev.DeveloperEvaluation.Application.Sales.Common;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.Common;

/// <summary>
/// Profile for mapping shared sale types between API and Application
/// </summary>
public class SaleResponseProfile : Profile
{
    public SaleResponseProfile()
    {
        CreateMap<SaleItemRequest, SaleItemCommand>();
        CreateMap<SaleItemResult, SaleItemResponse>();
        CreateMap<SaleResult, SaleResponse>();
    }
}
