using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

/// <summary>
/// Command for listing sales with filtering, ordering and pagination
/// </summary>
public class ListSalesCommand : IRequest<ListSalesResult>
{
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 10;
    public string? Order { get; set; }
    public string? SaleNumber { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public bool? IsCancelled { get; set; }
    public DateTime? MinSaleDate { get; set; }
    public DateTime? MaxSaleDate { get; set; }
    public decimal? MinTotalAmount { get; set; }
    public decimal? MaxTotalAmount { get; set; }
}
