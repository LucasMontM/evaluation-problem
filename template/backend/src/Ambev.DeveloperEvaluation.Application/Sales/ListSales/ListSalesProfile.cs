using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

/// <summary>
/// Profile for mapping ListSalesCommand to the repository filter
/// </summary>
public class ListSalesProfile : Profile
{
    public ListSalesProfile()
    {
        CreateMap<ListSalesCommand, SaleListFilter>();
    }
}
