using Ambev.DeveloperEvaluation.Application.Sales.Common;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

/// <summary>
/// Builds a real AutoMapper instance with the Sales profiles, so mappings are covered by the tests.
/// </summary>
internal static class SaleMapperFactory
{
    public static IMapper Create()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddMaps(typeof(SaleResultProfile).Assembly));
        return configuration.CreateMapper();
    }
}
