using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Sales.Common;

/// <summary>
/// Profile for mapping the Sale aggregate to SaleResult
/// </summary>
public class SaleResultProfile : Profile
{
    public SaleResultProfile()
    {
        CreateMap<SaleItem, SaleItemResult>();
        CreateMap<Sale, SaleResult>();
    }
}
