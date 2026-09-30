using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;

/// <summary>
/// Command for deleting a sale
/// </summary>
public record DeleteSaleCommand : IRequest<DeleteSaleResponse>
{
    public Guid Id { get; }

    public DeleteSaleCommand(Guid id)
    {
        Id = id;
    }
}
