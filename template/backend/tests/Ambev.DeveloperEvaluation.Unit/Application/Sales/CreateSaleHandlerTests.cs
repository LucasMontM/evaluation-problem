using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Application.Sales.TestData;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

/// <summary>
/// Contains unit tests for the <see cref="CreateSaleHandler"/> class.
/// </summary>
public class CreateSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly CreateSaleHandler _handler;

    public CreateSaleHandlerTests()
    {
        _saleRepository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Sale>());
        _handler = new CreateSaleHandler(_saleRepository, SaleMapperFactory.Create(), _mediator);
    }

    [Fact(DisplayName = "Given valid sale When creating Then persists, calculates totals and publishes SaleCreated")]
    public async Task Handle_ValidRequest_CreatesSale()
    {
        var command = SaleHandlerTestData.GenerateValidCreateCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        result.SaleNumber.Should().Be(command.SaleNumber);
        result.Items.Should().HaveCount(command.Items.Count);
        result.TotalAmount.Should().Be(result.Items.Sum(i => i.TotalAmount));
        await _saleRepository.Received(1).CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        await _mediator.Received(1).Publish(
            Arg.Is<SaleCreatedEvent>(e => e.SaleId == result.Id && e.TotalAmount == result.TotalAmount),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given 12 identical items When creating Then applies 20% discount")]
    public async Task Handle_TwelveItems_AppliesTwentyPercent()
    {
        var command = SaleHandlerTestData.GenerateValidCreateCommand();
        command.Items = [new() { ProductId = Guid.NewGuid(), ProductName = "Beer", Quantity = 12, UnitPrice = 5m }];

        var result = await _handler.Handle(command, CancellationToken.None);

        var item = result.Items.Single();
        item.DiscountPercentage.Should().Be(0.20m);
        item.Discount.Should().Be(12m);
        item.TotalAmount.Should().Be(48m);
        result.TotalAmount.Should().Be(48m);
    }

    [Fact(DisplayName = "Given more than 20 identical items When creating Then throws validation exception")]
    public async Task Handle_AboveMaxQuantity_ThrowsValidation()
    {
        var command = SaleHandlerTestData.GenerateValidCreateCommand();
        command.Items[0].Quantity = 21;

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
        await _saleRepository.DidNotReceive().CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given same product in two lines When creating Then throws validation exception")]
    public async Task Handle_DuplicatedProduct_ThrowsValidation()
    {
        var command = SaleHandlerTestData.GenerateValidCreateCommand();
        var productId = Guid.NewGuid();
        command.Items =
        [
            new() { ProductId = productId, ProductName = "Beer", Quantity = 15, UnitPrice = 5m },
            new() { ProductId = productId, ProductName = "Beer", Quantity = 15, UnitPrice = 5m }
        ];

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact(DisplayName = "Given existing sale number When creating Then throws InvalidOperationException")]
    public async Task Handle_DuplicatedSaleNumber_Throws()
    {
        var command = SaleHandlerTestData.GenerateValidCreateCommand();
        _saleRepository.ExistsBySaleNumberAsync(command.SaleNumber, Arg.Any<CancellationToken>()).Returns(true);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _mediator.DidNotReceive().Publish(Arg.Any<SaleCreatedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Given empty command When creating Then throws validation exception")]
    public async Task Handle_EmptyCommand_ThrowsValidation()
    {
        var act = () => _handler.Handle(new CreateSaleCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }
}
