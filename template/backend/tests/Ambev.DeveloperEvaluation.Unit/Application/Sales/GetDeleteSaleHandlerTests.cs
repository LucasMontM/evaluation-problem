using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

/// <summary>
/// Contains unit tests for <see cref="GetSaleHandler"/>, <see cref="ListSalesHandler"/> and <see cref="DeleteSaleHandler"/>.
/// </summary>
public class GetDeleteSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();

    [Fact(DisplayName = "Given existing sale When getting Then returns mapped sale")]
    public async Task GetSale_Existing_ReturnsSale()
    {
        var sale = SaleTestData.GenerateValidSaleWithItems(4, 10);
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        var handler = new GetSaleHandler(_saleRepository, SaleMapperFactory.Create());

        var result = await handler.Handle(new GetSaleCommand(sale.Id), CancellationToken.None);

        result.Id.Should().Be(sale.Id);
        result.TotalAmount.Should().Be(sale.TotalAmount);
        result.Items.Should().HaveCount(2);
    }

    [Fact(DisplayName = "Given unknown sale When getting Then throws KeyNotFoundException")]
    public async Task GetSale_Unknown_Throws()
    {
        var handler = new GetSaleHandler(_saleRepository, SaleMapperFactory.Create());

        var act = () => handler.Handle(new GetSaleCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "Given filter When listing Then forwards filter and returns page")]
    public async Task ListSales_ReturnsPage()
    {
        var sales = new List<Sale> { SaleTestData.GenerateValidSaleWithItems(1), SaleTestData.GenerateValidSaleWithItems(5) };
        _saleRepository.ListAsync(Arg.Any<SaleListFilter>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Sale>)sales, 12));
        var handler = new ListSalesHandler(_saleRepository, SaleMapperFactory.Create());

        var result = await handler.Handle(
            new ListSalesCommand { Page = 2, Size = 2, CustomerName = "John*", Order = "saleDate desc" },
            CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(12);
        result.Page.Should().Be(2);
        await _saleRepository.Received(1).ListAsync(
            Arg.Is<SaleListFilter>(f => f.Page == 2 && f.Size == 2 && f.CustomerName == "John*" && f.Order == "saleDate desc"),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given invalid page size When listing Then throws validation exception")]
    public async Task ListSales_InvalidSize_Throws()
    {
        var handler = new ListSalesHandler(_saleRepository, SaleMapperFactory.Create());

        var act = () => handler.Handle(new ListSalesCommand { Size = 0 }, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact(DisplayName = "Given existing sale When deleting Then returns success")]
    public async Task DeleteSale_Existing_ReturnsSuccess()
    {
        var id = Guid.NewGuid();
        _saleRepository.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new DeleteSaleHandler(_saleRepository);

        var result = await handler.Handle(new DeleteSaleCommand(id), CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact(DisplayName = "Given unknown sale When deleting Then throws KeyNotFoundException")]
    public async Task DeleteSale_Unknown_Throws()
    {
        _saleRepository.DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteSaleHandler(_saleRepository);

        var act = () => handler.Handle(new DeleteSaleCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
