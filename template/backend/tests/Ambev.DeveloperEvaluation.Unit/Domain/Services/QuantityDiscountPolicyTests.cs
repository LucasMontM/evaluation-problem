using Ambev.DeveloperEvaluation.Domain.Services;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Services;

/// <summary>
/// Contains unit tests for the quantity based discount rules.
/// </summary>
public class QuantityDiscountPolicyTests
{
    [Theory(DisplayName = "Given quantity When getting discount rate Then applies the correct tier")]
    [InlineData(1, 0)]
    [InlineData(3, 0)]
    [InlineData(4, 0.10)]
    [InlineData(9, 0.10)]
    [InlineData(10, 0.20)]
    [InlineData(20, 0.20)]
    public void GetDiscountRate_ValidQuantity_ReturnsTierRate(int quantity, double expectedRate)
    {
        QuantityDiscountPolicy.GetDiscountRate(quantity).Should().Be((decimal)expectedRate);
    }

    [Theory(DisplayName = "Given quantity above 20 or below 1 When getting discount rate Then throws DomainException")]
    [InlineData(21)]
    [InlineData(100)]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetDiscountRate_InvalidQuantity_Throws(int quantity)
    {
        var act = () => QuantityDiscountPolicy.GetDiscountRate(quantity);

        act.Should().Throw<DomainException>();
    }
}
