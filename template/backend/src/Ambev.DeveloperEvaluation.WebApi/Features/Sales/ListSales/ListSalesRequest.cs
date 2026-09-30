using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;

/// <summary>
/// Query parameters for listing sales, following the General API conventions
/// (_page, _size, _order, field filters with '*' wildcard, _min/_max ranges).
/// </summary>
public class ListSalesRequest
{
    [FromQuery(Name = "_page")] public int Page { get; set; } = 1;
    [FromQuery(Name = "_size")] public int Size { get; set; } = 10;
    [FromQuery(Name = "_order")] public string? Order { get; set; }

    [FromQuery(Name = "saleNumber")] public string? SaleNumber { get; set; }
    [FromQuery(Name = "customerId")] public Guid? CustomerId { get; set; }
    [FromQuery(Name = "customerName")] public string? CustomerName { get; set; }
    [FromQuery(Name = "branchId")] public Guid? BranchId { get; set; }
    [FromQuery(Name = "branchName")] public string? BranchName { get; set; }
    [FromQuery(Name = "isCancelled")] public bool? IsCancelled { get; set; }

    [FromQuery(Name = "_minSaleDate")] public DateTime? MinSaleDate { get; set; }
    [FromQuery(Name = "_maxSaleDate")] public DateTime? MaxSaleDate { get; set; }
    [FromQuery(Name = "_minTotalAmount")] public decimal? MinTotalAmount { get; set; }
    [FromQuery(Name = "_maxTotalAmount")] public decimal? MaxTotalAmount { get; set; }
}
