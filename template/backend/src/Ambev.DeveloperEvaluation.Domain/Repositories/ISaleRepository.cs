using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// Repository interface for Sale aggregate operations
/// </summary>
public interface ISaleRepository
{
    /// <summary>
    /// Creates a new sale with its items
    /// </summary>
    Task<Sale> CreateAsync(Sale sale, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a sale with its items by its unique identifier
    /// </summary>
    /// <returns>The sale if found, null otherwise</returns>
    Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a sale with the given number already exists
    /// </summary>
    Task<bool> ExistsBySaleNumberAsync(string saleNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists changes made to a sale previously retrieved by <see cref="GetByIdAsync"/>
    /// </summary>
    Task<Sale> UpdateAsync(Sale sale, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a sale and its items
    /// </summary>
    /// <returns>True if the sale was deleted, false if not found</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists sales with filtering, ordering and pagination
    /// </summary>
    /// <returns>The page of sales and the total count matching the filter</returns>
    Task<(IReadOnlyList<Sale> Items, int TotalCount)> ListAsync(SaleListFilter filter, CancellationToken cancellationToken = default);
}
