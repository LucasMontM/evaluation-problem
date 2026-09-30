namespace Ambev.DeveloperEvaluation.Domain.Services;

/// <summary>
/// Quantity-based discount rules applied to identical items (same product) in a sale.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>1 to 3 items: no discount</description></item>
/// <item><description>4 to 9 items: 10% discount</description></item>
/// <item><description>10 to 20 items: 20% discount</description></item>
/// <item><description>Above 20 items: not allowed</description></item>
/// </list>
/// </remarks>
public static class QuantityDiscountPolicy
{
    /// <summary>
    /// Minimum quantity of identical items that grants a discount.
    /// </summary>
    public const int MinQuantityForDiscount = 4;

    /// <summary>
    /// Minimum quantity of identical items that grants the higher discount tier.
    /// </summary>
    public const int MinQuantityForHigherDiscount = 10;

    /// <summary>
    /// Maximum quantity of identical items allowed in a single sale.
    /// </summary>
    public const int MaxQuantityPerProduct = 20;

    /// <summary>
    /// Returns the discount rate (0.00 to 1.00) for the given quantity.
    /// </summary>
    /// <param name="quantity">Quantity of identical items</param>
    /// <returns>The discount rate to apply</returns>
    /// <exception cref="DomainException">When quantity is below 1 or above the maximum allowed</exception>
    public static decimal GetDiscountRate(int quantity)
    {
        if (quantity < 1)
            throw new DomainException("Quantity must be at least 1.");

        if (quantity > MaxQuantityPerProduct)
            throw new DomainException($"It is not possible to sell more than {MaxQuantityPerProduct} identical items.");

        if (quantity >= MinQuantityForHigherDiscount)
            return 0.20m;

        if (quantity >= MinQuantityForDiscount)
            return 0.10m;

        return 0m;
    }
}
