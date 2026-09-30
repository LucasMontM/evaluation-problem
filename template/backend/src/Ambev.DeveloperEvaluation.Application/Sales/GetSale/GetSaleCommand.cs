using Ambev.DeveloperEvaluation.Application.Sales.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSale;

/// <summary>
/// Command for retrieving a sale by its ID
/// </summary>
public record GetSaleCommand : IRequest<SaleResult>
{
    public Guid Id { get; }

    public GetSaleCommand(Guid id)
    {
        Id = id;
    }
}
