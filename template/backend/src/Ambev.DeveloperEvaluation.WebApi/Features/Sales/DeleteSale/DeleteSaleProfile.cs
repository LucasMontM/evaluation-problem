using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.DeleteSale;

/// <summary>
/// Profile for mapping DeleteSale feature requests to commands
/// </summary>
public class DeleteSaleProfile : Profile
{
    public DeleteSaleProfile()
    {
        CreateMap<Guid, DeleteSaleCommand>()
            .ConstructUsing(id => new DeleteSaleCommand(id));
    }
}
