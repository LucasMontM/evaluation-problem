namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.Common;

/// <summary>
/// Product line of a create/update sale request
/// </summary>
public class SaleItemRequest
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
