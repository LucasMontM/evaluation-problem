using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

/// <summary>
/// Implementation of ISaleRepository using Entity Framework Core
/// </summary>
public class SaleRepository : ISaleRepository
{
    /// <summary>
    /// JSON field names accepted by "_order", mapped to entity properties.
    /// </summary>
    private static readonly Dictionary<string, string> OrderableFields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["saleNumber"] = nameof(Sale.SaleNumber),
        ["saleDate"] = nameof(Sale.SaleDate),
        ["customerName"] = nameof(Sale.CustomerName),
        ["branchName"] = nameof(Sale.BranchName),
        ["totalAmount"] = nameof(Sale.TotalAmount),
        ["isCancelled"] = nameof(Sale.IsCancelled),
        ["createdAt"] = nameof(Sale.CreatedAt)
    };

    private readonly DefaultContext _context;

    public SaleRepository(DefaultContext context)
    {
        _context = context;
    }

    public async Task<Sale> CreateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        await _context.Sales.AddAsync(sale, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    public async Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsBySaleNumberAsync(string saleNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Sales.AnyAsync(s => s.SaleNumber == saleNumber, cancellationToken);
    }

    public async Task<Sale> UpdateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await _context.Sales.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (sale == null)
            return false;

        _context.Sales.Remove(sale);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(IReadOnlyList<Sale> Items, int TotalCount)> ListAsync(SaleListFilter filter, CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(_context.Sales.AsNoTracking(), filter);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await ApplyOrder(query, filter.Order)
            .Include(s => s.Items)
            .AsSplitQuery()
            .Skip((filter.Page - 1) * filter.Size)
            .Take(filter.Size)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private static IQueryable<Sale> ApplyFilters(IQueryable<Sale> query, SaleListFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.SaleNumber))
            query = query.Where(s => EF.Functions.ILike(s.SaleNumber, ToLikePattern(filter.SaleNumber), "\\"));

        if (!string.IsNullOrWhiteSpace(filter.CustomerName))
            query = query.Where(s => EF.Functions.ILike(s.CustomerName, ToLikePattern(filter.CustomerName), "\\"));

        if (!string.IsNullOrWhiteSpace(filter.BranchName))
            query = query.Where(s => EF.Functions.ILike(s.BranchName, ToLikePattern(filter.BranchName), "\\"));

        if (filter.CustomerId.HasValue)
            query = query.Where(s => s.CustomerId == filter.CustomerId.Value);

        if (filter.BranchId.HasValue)
            query = query.Where(s => s.BranchId == filter.BranchId.Value);

        if (filter.IsCancelled.HasValue)
            query = query.Where(s => s.IsCancelled == filter.IsCancelled.Value);

        if (filter.MinSaleDate.HasValue)
        {
            var min = ToUtc(filter.MinSaleDate.Value);
            query = query.Where(s => s.SaleDate >= min);
        }

        if (filter.MaxSaleDate.HasValue)
        {
            var max = ToUtc(filter.MaxSaleDate.Value);
            query = query.Where(s => s.SaleDate <= max);
        }

        if (filter.MinTotalAmount.HasValue)
            query = query.Where(s => s.TotalAmount >= filter.MinTotalAmount.Value);

        if (filter.MaxTotalAmount.HasValue)
            query = query.Where(s => s.TotalAmount <= filter.MaxTotalAmount.Value);

        return query;
    }

    /// <summary>
    /// Applies "_order" (e.g. "saleDate desc, saleNumber"). Unknown fields are ignored.
    /// Defaults to saleDate descending. Id is always the final tie-breaker for stable paging.
    /// </summary>
    private static IQueryable<Sale> ApplyOrder(IQueryable<Sale> query, string? order)
    {
        IOrderedQueryable<Sale>? ordered = null;

        if (!string.IsNullOrWhiteSpace(order))
        {
            foreach (var clause in order.Trim('"', '\'').Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parts = clause.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (!OrderableFields.TryGetValue(parts[0], out var property))
                    continue;

                var descending = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

                ordered = ordered is null
                    ? (descending
                        ? query.OrderByDescending(s => EF.Property<object>(s, property))
                        : query.OrderBy(s => EF.Property<object>(s, property)))
                    : (descending
                        ? ordered.ThenByDescending(s => EF.Property<object>(s, property))
                        : ordered.ThenBy(s => EF.Property<object>(s, property)));
            }
        }

        ordered ??= query.OrderByDescending(s => s.SaleDate);
        return ordered.ThenBy(s => s.Id);
    }

    /// <summary>
    /// Converts the API wildcard syntax ("abc*", "*abc", "*abc*") into an ILIKE pattern.
    /// </summary>
    private static string ToLikePattern(string value)
    {
        var startsWithWildcard = value.StartsWith('*');
        var endsWithWildcard = value.Length > 1 && value.EndsWith('*');

        var core = value.Trim('*')
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");

        return $"{(startsWithWildcard ? "%" : string.Empty)}{core}{(endsWithWildcard ? "%" : string.Empty)}";
    }

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
