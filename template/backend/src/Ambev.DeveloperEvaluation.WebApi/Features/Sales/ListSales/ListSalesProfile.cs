using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;

/// <summary>
/// Profile for mapping ListSales feature requests to commands
/// </summary>
public class ListSalesProfile : Profile
{
    public ListSalesProfile()
    {
        CreateMap<ListSalesRequest, ListSalesCommand>();
    }
}
