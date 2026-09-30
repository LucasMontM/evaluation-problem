using Ambev.DeveloperEvaluation.Domain.Entities;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;

/// <summary>
/// Generates Sale aggregates with valid random data using Bogus.
/// </summary>
public static class SaleTestData
{
    private static readonly Faker Faker = new();

    /// <summary>
    /// Generates a valid sale without items.
    /// </summary>
    public static Sale GenerateValidSale()
    {
        return new Sale(
            $"S-{Faker.Random.AlphaNumeric(8).ToUpperInvariant()}",
            Faker.Date.Recent().ToUniversalTime(),
            Guid.NewGuid(),
            Faker.Name.FullName(),
            Guid.NewGuid(),
            Faker.Company.CompanyName());
    }

    /// <summary>
    /// Generates a valid sale with the given quantities, one product per quantity.
    /// </summary>
    public static Sale GenerateValidSaleWithItems(params int[] quantities)
    {
        var sale = GenerateValidSale();
        foreach (var quantity in quantities)
            sale.AddItem(Guid.NewGuid(), Faker.Commerce.ProductName(), quantity, GenerateUnitPrice());
        return sale;
    }

    /// <summary>
    /// Generates a valid unit price with two decimal places.
    /// </summary>
    public static decimal GenerateUnitPrice()
    {
        return Math.Round(Faker.Random.Decimal(1, 500), 2);
    }
}
