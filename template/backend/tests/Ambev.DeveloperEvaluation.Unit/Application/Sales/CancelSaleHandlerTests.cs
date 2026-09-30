using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

/// <summary>
/// Contains unit tests for <see cref="CancelSaleHandler"/> and <see cref="CancelSaleItemHandler"/>.
/// </summary>
public class CancelSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    public CancelSaleHandlerTests()
    {
        _saleRepository.UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Sale>());
    }

    [Fact(DisplayName = "Given active sale When cancelling Then sale is cancelled and SaleCancelled is published")]
    public async Task CancelSale_ActiveSale_Cancels()
    {
        var sale = SaleTestData.GenerateValidSaleWithItems(5);
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        var handler = new CancelSaleHandler(_saleRepository, SaleMapperFactory.Create(), _mediator);

        var result = await handler.Handle(new CancelSaleCommand(sale.Id), CancellationToken.None);

        result.IsCancelled.Should().BeTrue();
        await _mediator.Received(1).Publish(Arg.Is<SaleCancelledEvent>(e => e.SaleId == sale.Id), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given unknown sale When cancelling Then throws KeyNotFoundException")]
    public async Task CancelSale_UnknownSale_Throws()
    {
        var handler = new CancelSaleHandler(_saleRepository, SaleMapperFactory.Create(), _mediator);

        var act = () => handler.Handle(new CancelSaleCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "Given sale item When cancelling item Then total is recalculated and ItemCancelled is published")]
    public async Task CancelSaleItem_ExistingItem_Cancels()
    {
        var sale = SaleTestData.GenerateValidSaleWithItems(2, 12);
        var item = sale.Items.Last();
        var expectedTotal = sale.TotalAmount - item.TotalAmount;
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        var handler = new CancelSaleItemHandler(_saleRepository, SaleMapperFactory.Create(), _mediator);

        var result = await handler.Handle(new CancelSaleItemCommand(sale.Id, item.Id), CancellationToken.None);

        result.TotalAmount.Should().Be(expectedTotal);
        result.Items.Single(i => i.Id == item.Id).IsCancelled.Should().BeTrue();
        await _mediator.Received(1).Publish(
            Arg.Is<ItemCancelledEvent>(e => e.SaleId == sale.Id && e.ItemId == item.Id && e.SaleTotalAmount == expectedTotal),
            Arg.Any<CancellationToken>());
    }
}
