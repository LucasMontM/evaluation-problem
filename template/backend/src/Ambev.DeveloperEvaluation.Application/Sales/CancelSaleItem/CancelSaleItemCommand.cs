using Ambev.DeveloperEvaluation.Application.Sales.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;

/// <summary>
/// Command for cancelling a single item of a sale
/// </summary>
public record CancelSaleItemCommand : IRequest<SaleResult>
{
    public Guid SaleId { get; }
    public Guid ItemId { get; }

    public CancelSaleItemCommand(Guid saleId, Guid itemId)
    {
        SaleId = saleId;
        ItemId = itemId;
    }
}
