using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Services;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Represents a product line in a sale.
/// The product is referenced through the External Identities pattern (ProductId)
/// with its description denormalized (ProductName).
/// </summary>
public class SaleItem : BaseEntity
{
    /// <summary>
    /// Identifier of the sale this item belongs to.
    /// </summary>
    public Guid SaleId { get; private set; }

    /// <summary>
    /// External identity of the product (Products domain).
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// Denormalized product name at the moment of the sale.
    /// </summary>
    public string ProductName { get; private set; } = string.Empty;

    /// <summary>
    /// Quantity of identical items.
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// Unit price of the product.
    /// </summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>
    /// Discount rate applied (0.00, 0.10 or 0.20).
    /// </summary>
    public decimal DiscountPercentage { get; private set; }

    /// <summary>
    /// Discount amount applied to this item.
    /// </summary>
    public decimal Discount { get; private set; }

    /// <summary>
    /// Total amount of this item after discount.
    /// </summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>
    /// Indicates whether this item was cancelled.
    /// Cancelled items do not count towards the sale total.
    /// </summary>
    public bool IsCancelled { get; private set; }

    /// <summary>
    /// Constructor used by EF Core.
    /// </summary>
    private SaleItem() { }

    /// <summary>
    /// Creates a new sale item and calculates its discount and total.
    /// </summary>
    internal SaleItem(Guid saleId, Guid productId, string productName, int quantity, decimal unitPrice)
    {
        if (productId == Guid.Empty)
            throw new DomainException("Product id is required.");

        Id = Guid.NewGuid();
        SaleId = saleId;
        ProductId = productId;
        Update(productName, quantity, unitPrice);
    }

    /// <summary>
    /// Updates name, quantity and unit price, recalculating discount and total.
    /// </summary>
    internal void Update(string productName, int quantity, decimal unitPrice)
    {
        if (IsCancelled)
            throw new DomainException($"Item {Id} is cancelled and cannot be modified.");

        if (string.IsNullOrWhiteSpace(productName))
            throw new DomainException("Product name is required.");

        if (unitPrice <= 0)
            throw new DomainException("Unit price must be greater than zero.");

        var discountRate = QuantityDiscountPolicy.GetDiscountRate(quantity);
        var grossAmount = quantity * unitPrice;

        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        DiscountPercentage = discountRate;
        Discount = Math.Round(grossAmount * discountRate, 2, MidpointRounding.AwayFromZero);
        TotalAmount = grossAmount - Discount;
    }

    /// <summary>
    /// Cancels this item.
    /// </summary>
    internal void Cancel()
    {
        if (IsCancelled)
            throw new DomainException($"Item {Id} is already cancelled.");

        IsCancelled = true;
    }
}
