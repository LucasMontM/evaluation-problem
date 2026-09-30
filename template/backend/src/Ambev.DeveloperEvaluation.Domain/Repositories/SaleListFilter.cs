namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// Filtering, ordering and pagination parameters for listing sales.
/// String filters accept '*' at the start and/or end for partial matching.
/// </summary>
public sealed class SaleListFilter
{
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 10;

    /// <summary>
    /// Ordering expression, e.g. "saleDate desc, saleNumber asc".
    /// </summary>
    public string? Order { get; init; }

    public string? SaleNumber { get; init; }
    public Guid? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public Guid? BranchId { get; init; }
    public string? BranchName { get; init; }
    public bool? IsCancelled { get; init; }
    public DateTime? MinSaleDate { get; init; }
    public DateTime? MaxSaleDate { get; init; }
    public decimal? MinTotalAmount { get; init; }
    public decimal? MaxTotalAmount { get; init; }
}
