using Ambev.DeveloperEvaluation.Application.Sales.Common;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

/// <summary>
/// Page of sales returned by ListSalesCommand
/// </summary>
public class ListSalesResult
{
    public List<SaleResult> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int Size { get; set; }
}
