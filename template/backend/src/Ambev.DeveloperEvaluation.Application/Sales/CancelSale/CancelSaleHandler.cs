using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSale;

/// <summary>
/// Handler for processing CancelSaleCommand requests
/// </summary>
public class CancelSaleHandler : IRequestHandler<CancelSaleCommand, SaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;

    public CancelSaleHandler(ISaleRepository saleRepository, IMapper mapper, IMediator mediator)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _mediator = mediator;
    }

    public async Task<SaleResult> Handle(CancelSaleCommand request, CancellationToken cancellationToken)
    {
        var validator = new CancelSaleValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var sale = await _saleRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sale with ID {request.Id} not found");

        sale.Cancel();

        var updatedSale = await _saleRepository.UpdateAsync(sale, cancellationToken);

        await _mediator.Publish(new SaleCancelledEvent(
            updatedSale.Id,
            updatedSale.SaleNumber,
            DateTime.UtcNow), cancellationToken);

        return _mapper.Map<SaleResult>(updatedSale);
    }
}
