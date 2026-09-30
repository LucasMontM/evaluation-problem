using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

/// <summary>
/// Contains unit tests for the Sale aggregate.
/// </summary>
public class SaleTests
{
    [Theory(DisplayName = "Given item quantity When adding item Then discount and total are calculated")]
    [InlineData(3, 10.00, 0, 0.00, 30.00)]
    [InlineData(4, 10.00, 0.10, 4.00, 36.00)]
    [InlineData(10, 10.00, 0.20, 20.00, 80.00)]
    [InlineData(20, 2.50, 0.20, 10.00, 40.00)]
    public void AddItem_CalculatesDiscountAndTotal(int quantity, double unitPrice, double rate, double discount, double total)
    {
        var sale = SaleTestData.GenerateValidSale();

        var item = sale.AddItem(Guid.NewGuid(), "Product", quantity, (decimal)unitPrice);

        item.DiscountPercentage.Should().Be((decimal)rate);
        item.Discount.Should().Be((decimal)discount);
        item.TotalAmount.Should().Be((decimal)total);
        sale.TotalAmount.Should().Be((decimal)total);
    }

    [Fact(DisplayName = "Given more than 20 identical items When adding item Then throws DomainException")]
    public void AddItem_AboveMaxQuantity_Throws()
    {
        var sale = SaleTestData.GenerateValidSale();

        var act = () => sale.AddItem(Guid.NewGuid(), "Product", 21, 10m);

        act.Should().Throw<DomainException>();
        sale.Items.Should().BeEmpty();
    }

    [Fact(DisplayName = "Given product already in sale When adding same product Then throws DomainException")]
    public void AddItem_DuplicatedProduct_Throws()
    {
        var sale = SaleTestData.GenerateValidSale();
        var productId = Guid.NewGuid();
        sale.AddItem(productId, "Product", 15, 10m);

        var act = () => sale.AddItem(productId, "Product", 10, 10m);

        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "Given multiple items When adding Then sale total is the sum of item totals")]
    public void AddItem_Multiple_SumsTotal()
    {
        var sale = SaleTestData.GenerateValidSale();

        sale.AddItem(Guid.NewGuid(), "A", 2, 10m);  // 20.00
        sale.AddItem(Guid.NewGuid(), "B", 5, 10m);  // 45.00
        sale.AddItem(Guid.NewGuid(), "C", 10, 10m); // 80.00

        sale.TotalAmount.Should().Be(145.00m);
    }

    [Fact(DisplayName = "Given an item When cancelling it Then it is excluded from the total")]
    public void CancelItem_RemovesFromTotal()
    {
        var sale = SaleTestData.GenerateValidSale();
        var kept = sale.AddItem(Guid.NewGuid(), "A", 2, 10m);
        var cancelled = sale.AddItem(Guid.NewGuid(), "B", 5, 10m);

        sale.CancelItem(cancelled.Id);

        cancelled.IsCancelled.Should().BeTrue();
        sale.TotalAmount.Should().Be(kept.TotalAmount);
        sale.UpdatedAt.Should().NotBeNull();
    }

    [Fact(DisplayName = "Given unknown item When cancelling it Then throws KeyNotFoundException")]
    public void CancelItem_UnknownItem_Throws()
    {
        var sale = SaleTestData.GenerateValidSaleWithItems(2);

        var act = () => sale.CancelItem(Guid.NewGuid());

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact(DisplayName = "Given cancelled item When cancelling it again Then throws DomainException")]
    public void CancelItem_AlreadyCancelled_Throws()
    {
        var sale = SaleTestData.GenerateValidSaleWithItems(2, 3);
        var itemId = sale.Items.First().Id;
        sale.CancelItem(itemId);

        var act = () => sale.CancelItem(itemId);

        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "Given active sale When cancelling Then sale is cancelled")]
    public void Cancel_ActiveSale_SetsCancelled()
    {
        var sale = SaleTestData.GenerateValidSaleWithItems(5);

        sale.Cancel();

        sale.IsCancelled.Should().BeTrue();
        sale.UpdatedAt.Should().NotBeNull();
    }

    [Fact(DisplayName = "Given cancelled sale When modifying Then throws DomainException")]
    public void CancelledSale_CannotBeModified()
    {
        var sale = SaleTestData.GenerateValidSaleWithItems(5);
        sale.Cancel();

        sale.Invoking(s => s.Cancel()).Should().Throw<DomainException>();
        sale.Invoking(s => s.AddItem(Guid.NewGuid(), "X", 1, 1m)).Should().Throw<DomainException>();
        sale.Invoking(s => s.CancelItem(s.Items.First().Id)).Should().Throw<DomainException>();
        sale.Invoking(s => s.Update(s.SaleDate, s.CustomerId, s.CustomerName, s.BranchId, s.BranchName, []))
            .Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "Given new item lines When updating Then items are updated, added and removed by product")]
    public void Update_SynchronizesItemsByProduct()
    {
        var sale = SaleTestData.GenerateValidSale();
        var updatedProduct = Guid.NewGuid();
        var removedProduct = Guid.NewGuid();
        var addedProduct = Guid.NewGuid();
        var updatedItem = sale.AddItem(updatedProduct, "Updated", 2, 10m);
        sale.AddItem(removedProduct, "Removed", 2, 10m);

        sale.Update(sale.SaleDate, sale.CustomerId, "New Customer", sale.BranchId, sale.BranchName,
        [
            (updatedProduct, "Updated", 10, 10m),
            (addedProduct, "Added", 4, 5m)
        ]);

        sale.Items.Should().HaveCount(2);
        sale.Items.Should().NotContain(i => i.ProductId == removedProduct);
        sale.Items.Single(i => i.ProductId == updatedProduct).Id.Should().Be(updatedItem.Id);
        updatedItem.Quantity.Should().Be(10);
        updatedItem.TotalAmount.Should().Be(80m);
        sale.TotalAmount.Should().Be(80m + 18m);
        sale.CustomerName.Should().Be("New Customer");
    }

    [Fact(DisplayName = "Given more than 20 identical items When updating Then throws DomainException")]
    public void Update_AboveMaxQuantity_Throws()
    {
        var sale = SaleTestData.GenerateValidSaleWithItems(2);
        var productId = sale.Items.First().ProductId;

        var act = () => sale.Update(sale.SaleDate, sale.CustomerId, sale.CustomerName, sale.BranchId, sale.BranchName,
            [(productId, "P", 21, 10m)]);

        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "Given unspecified date kind When creating sale Then date is stored as UTC")]
    public void Create_UnspecifiedDate_StoredAsUtc()
    {
        var sale = new Ambev.DeveloperEvaluation.Domain.Entities.Sale(
            "S-1", new DateTime(2026, 9, 30, 10, 0, 0, DateTimeKind.Unspecified),
            Guid.NewGuid(), "Customer", Guid.NewGuid(), "Branch");

        sale.SaleDate.Kind.Should().Be(DateTimeKind.Utc);
    }
}
