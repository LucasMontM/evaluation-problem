using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

/// <summary>
/// Handler for processing ListSalesCommand requests
/// </summary>
public class ListSalesHandler : IRequestHandler<ListSalesCommand, ListSalesResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;

    public ListSalesHandler(ISaleRepository saleRepository, IMapper mapper)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
    }

    public async Task<ListSalesResult> Handle(ListSalesCommand request, CancellationToken cancellationToken)
    {
        var validator = new ListSalesValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var filter = _mapper.Map<SaleListFilter>(request);
        var (items, totalCount) = await _saleRepository.ListAsync(filter, cancellationToken);

        return new ListSalesResult
        {
            Items = _mapper.Map<List<SaleResult>>(items),
            TotalCount = totalCount,
            Page = request.Page,
            Size = request.Size
        };
    }
}
