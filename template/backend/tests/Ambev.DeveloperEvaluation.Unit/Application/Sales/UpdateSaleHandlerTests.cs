using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Application.Sales.TestData;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

/// <summary>
/// Contains unit tests for the <see cref="UpdateSaleHandler"/> class.
/// </summary>
public class UpdateSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly UpdateSaleHandler _handler;

    public UpdateSaleHandlerTests()
    {
        _saleRepository.UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Sale>());
        _handler = new UpdateSaleHandler(_saleRepository, SaleMapperFactory.Create(), _mediator);
    }

    [Fact(DisplayName = "Given existing sale When updating Then persists changes and publishes SaleModified")]
    public async Task Handle_ExistingSale_Updates()
    {
        var sale = SaleTestData.GenerateValidSaleWithItems(3);
        var command = SaleHandlerTestData.GenerateValidUpdateCommand();
        command.Id = sale.Id;
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.CustomerName.Should().Be(command.CustomerName);
        result.BranchName.Should().Be(command.BranchName);
        result.Items.Select(i => i.ProductId).Should().BeEquivalentTo(command.Items.Select(i => i.ProductId));
        await _saleRepository.Received(1).UpdateAsync(sale, Arg.Any<CancellationToken>());
        await _mediator.Received(1).Publish(Arg.Is<SaleModifiedEvent>(e => e.SaleId == sale.Id), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given unknown sale When updating Then throws KeyNotFoundException")]
    public async Task Handle_UnknownSale_Throws()
    {
        var command = SaleHandlerTestData.GenerateValidUpdateCommand();
        _saleRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns((Sale?)null);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "Given cancelled sale When updating Then throws DomainException")]
    public async Task Handle_CancelledSale_Throws()
    {
        var sale = SaleTestData.GenerateValidSaleWithItems(3);
        sale.Cancel();
        var command = SaleHandlerTestData.GenerateValidUpdateCommand();
        command.Id = sale.Id;
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        await _saleRepository.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }
}
