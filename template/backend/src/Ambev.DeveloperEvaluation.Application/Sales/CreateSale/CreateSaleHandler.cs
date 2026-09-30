using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

/// <summary>
/// Handler for processing CreateSaleCommand requests
/// </summary>
public class CreateSaleHandler : IRequestHandler<CreateSaleCommand, SaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;

    public CreateSaleHandler(ISaleRepository saleRepository, IMapper mapper, IMediator mediator)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _mediator = mediator;
    }

    public async Task<SaleResult> Handle(CreateSaleCommand command, CancellationToken cancellationToken)
    {
        var validator = new CreateSaleCommandValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        if (await _saleRepository.ExistsBySaleNumberAsync(command.SaleNumber, cancellationToken))
            throw new InvalidOperationException($"Sale with number {command.SaleNumber} already exists");

        var sale = new Sale(
            command.SaleNumber,
            command.SaleDate,
            command.CustomerId,
            command.CustomerName,
            command.BranchId,
            command.BranchName);

        foreach (var item in command.Items)
            sale.AddItem(item.ProductId, item.ProductName, item.Quantity, item.UnitPrice);

        var createdSale = await _saleRepository.CreateAsync(sale, cancellationToken);

        await _mediator.Publish(new SaleCreatedEvent(
            createdSale.Id,
            createdSale.SaleNumber,
            createdSale.CustomerId,
            createdSale.BranchId,
            createdSale.TotalAmount,
            DateTime.UtcNow), cancellationToken);

        return _mapper.Map<SaleResult>(createdSale);
    }
}
