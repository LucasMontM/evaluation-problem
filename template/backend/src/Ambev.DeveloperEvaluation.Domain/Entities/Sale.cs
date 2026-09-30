using Ambev.DeveloperEvaluation.Domain.Common;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Sale aggregate root.
/// Customer and Branch are referenced through the External Identities pattern
/// (CustomerId / BranchId) with their descriptions denormalized (CustomerName / BranchName).
/// </summary>
public class Sale : BaseEntity
{
    private readonly List<SaleItem> _items = [];

    /// <summary>
    /// Business number of the sale. Unique.
    /// </summary>
    public string SaleNumber { get; private set; } = string.Empty;

    /// <summary>
    /// Date when the sale was made (UTC).
    /// </summary>
    public DateTime SaleDate { get; private set; }

    /// <summary>
    /// External identity of the customer (Customers domain).
    /// </summary>
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// Denormalized customer name.
    /// </summary>
    public string CustomerName { get; private set; } = string.Empty;

    /// <summary>
    /// External identity of the branch where the sale was made.
    /// </summary>
    public Guid BranchId { get; private set; }

    /// <summary>
    /// Denormalized branch name.
    /// </summary>
    public string BranchName { get; private set; } = string.Empty;

    /// <summary>
    /// Total amount of the sale: sum of the totals of non-cancelled items.
    /// </summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>
    /// Indicates whether the sale was cancelled.
    /// </summary>
    public bool IsCancelled { get; private set; }

    /// <summary>
    /// Date and time when the record was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Date and time of the last update (UTC).
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Items of the sale.
    /// </summary>
    public IReadOnlyCollection<SaleItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Constructor used by EF Core.
    /// </summary>
    private Sale() { }

    /// <summary>
    /// Creates a new sale without items.
    /// </summary>
    public Sale(string saleNumber, DateTime saleDate, Guid customerId, string customerName, Guid branchId, string branchName)
    {
        if (string.IsNullOrWhiteSpace(saleNumber))
            throw new DomainException("Sale number is required.");

        Id = Guid.NewGuid();
        SaleNumber = saleNumber;
        CreatedAt = DateTime.UtcNow;
        SetHeader(saleDate, customerId, customerName, branchId, branchName);
    }

    /// <summary>
    /// Adds a product line to the sale.
    /// Identical items must be informed in a single line, so the quantity limit per product cannot be bypassed.
    /// </summary>
    public SaleItem AddItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        EnsureNotCancelled();

        if (_items.Any(i => !i.IsCancelled && i.ProductId == productId))
            throw new DomainException($"Product {productId} is already in the sale. Identical items must be informed in a single line.");

        var item = new SaleItem(Id, productId, productName, quantity, unitPrice);
        _items.Add(item);
        RecalculateTotal();
        return item;
    }

    /// <summary>
    /// Updates the sale header and synchronizes its active items with the informed lines:
    /// existing products are updated, new products are added and missing products are removed.
    /// Cancelled items are kept as history.
    /// </summary>
    public void Update(
        DateTime saleDate,
        Guid customerId,
        string customerName,
        Guid branchId,
        string branchName,
        IReadOnlyCollection<(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice)> items)
    {
        EnsureNotCancelled();

        if (items.GroupBy(i => i.ProductId).Any(g => g.Count() > 1))
            throw new DomainException("Identical items must be informed in a single line.");

        SetHeader(saleDate, customerId, customerName, branchId, branchName);

        var activeItems = _items.Where(i => !i.IsCancelled).ToList();

        foreach (var removed in activeItems.Where(a => items.All(i => i.ProductId != a.ProductId)))
            _items.Remove(removed);

        foreach (var line in items)
        {
            var existing = activeItems.FirstOrDefault(a => a.ProductId == line.ProductId);
            if (existing is null)
                _items.Add(new SaleItem(Id, line.ProductId, line.ProductName, line.Quantity, line.UnitPrice));
            else
                existing.Update(line.ProductName, line.Quantity, line.UnitPrice);
        }

        RecalculateTotal();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancels the sale.
    /// </summary>
    public void Cancel()
    {
        EnsureNotCancelled();

        IsCancelled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancels a single item of the sale and recalculates the total.
    /// </summary>
    public SaleItem CancelItem(Guid itemId)
    {
        EnsureNotCancelled();

        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new KeyNotFoundException($"Item with ID {itemId} not found in sale {Id}");

        item.Cancel();
        RecalculateTotal();
        UpdatedAt = DateTime.UtcNow;
        return item;
    }

    private void SetHeader(DateTime saleDate, Guid customerId, string customerName, Guid branchId, string branchName)
    {
        if (customerId == Guid.Empty)
            throw new DomainException("Customer id is required.");

        if (string.IsNullOrWhiteSpace(customerName))
            throw new DomainException("Customer name is required.");

        if (branchId == Guid.Empty)
            throw new DomainException("Branch id is required.");

        if (string.IsNullOrWhiteSpace(branchName))
            throw new DomainException("Branch name is required.");

        SaleDate = saleDate.Kind switch
        {
            DateTimeKind.Utc => saleDate,
            DateTimeKind.Local => saleDate.ToUniversalTime(),
            _ => DateTime.SpecifyKind(saleDate, DateTimeKind.Utc)
        };
        CustomerId = customerId;
        CustomerName = customerName;
        BranchId = branchId;
        BranchName = branchName;
    }

    private void RecalculateTotal()
    {
        TotalAmount = _items.Where(i => !i.IsCancelled).Sum(i => i.TotalAmount);
    }

    private void EnsureNotCancelled()
    {
        if (IsCancelled)
            throw new DomainException($"Sale {SaleNumber} is cancelled and cannot be modified.");
    }
}
