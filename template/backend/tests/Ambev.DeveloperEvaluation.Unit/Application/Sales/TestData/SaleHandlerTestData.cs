using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.TestData;

/// <summary>
/// Generates valid sale commands using Bogus.
/// </summary>
public static class SaleHandlerTestData
{
    private static readonly Faker<SaleItemCommand> ItemFaker = new Faker<SaleItemCommand>()
        .RuleFor(i => i.ProductId, _ => Guid.NewGuid())
        .RuleFor(i => i.ProductName, f => f.Commerce.ProductName())
        .RuleFor(i => i.Quantity, f => f.Random.Int(1, 20))
        .RuleFor(i => i.UnitPrice, f => Math.Round(f.Random.Decimal(1, 500), 2));

    private static readonly Faker<CreateSaleCommand> CreateFaker = new Faker<CreateSaleCommand>()
        .RuleFor(s => s.SaleNumber, f => $"S-{f.Random.AlphaNumeric(8).ToUpperInvariant()}")
        .RuleFor(s => s.SaleDate, f => f.Date.Recent().ToUniversalTime())
        .RuleFor(s => s.CustomerId, _ => Guid.NewGuid())
        .RuleFor(s => s.CustomerName, f => f.Name.FullName())
        .RuleFor(s => s.BranchId, _ => Guid.NewGuid())
        .RuleFor(s => s.BranchName, f => f.Company.CompanyName())
        .RuleFor(s => s.Items, f => ItemFaker.Generate(f.Random.Int(1, 5)));

    private static readonly Faker<UpdateSaleCommand> UpdateFaker = new Faker<UpdateSaleCommand>()
        .RuleFor(s => s.Id, _ => Guid.NewGuid())
        .RuleFor(s => s.SaleDate, f => f.Date.Recent().ToUniversalTime())
        .RuleFor(s => s.CustomerId, _ => Guid.NewGuid())
        .RuleFor(s => s.CustomerName, f => f.Name.FullName())
        .RuleFor(s => s.BranchId, _ => Guid.NewGuid())
        .RuleFor(s => s.BranchName, f => f.Company.CompanyName())
        .RuleFor(s => s.Items, f => ItemFaker.Generate(f.Random.Int(1, 5)));

    public static CreateSaleCommand GenerateValidCreateCommand() => CreateFaker.Generate();

    public static UpdateSaleCommand GenerateValidUpdateCommand() => UpdateFaker.Generate();
}
