namespace Ambev.DeveloperEvaluation.Application.Sales.Common;

/// <summary>
/// Product line informed when creating or updating a sale
/// </summary>
public class SaleItemCommand
{
    /// <summary>
    /// External identity of the product
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Denormalized product name
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Quantity of identical items (1 to 20)
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Unit price of the product
    /// </summary>
    public decimal UnitPrice { get; set; }
}
