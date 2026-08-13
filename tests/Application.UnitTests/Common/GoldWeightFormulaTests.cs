using Domain.Common;
using Xunit;

namespace Application.UnitTests.Common;

/// <summary>
/// Pins the store's 21K-equivalent conversion convention (AGENTS.md Critical Business
/// Rules). The formula deviates from a generic weight * karat / 21 for 18K, which is
/// intentionally 700/875 (= 0.8) per the store's convention; this test locks it in so
/// the behavior cannot regress silently.
/// </summary>
public sealed class GoldWeightFormulaTests
{
    [Theory]
    [InlineData(100, Karat.K21, 100)]
    [InlineData(100, Karat.K24, 114.286)]
    [InlineData(100, Karat.K18, 80)]
    [InlineData(1, Karat.K24, 1.143)]
    [InlineData(1, Karat.K18, 0.8)]
    [InlineData(3.5, Karat.K24, 4)]
    public void CalculateEquivalent21KWeight_MatchesStoreConvention(decimal weightInGrams, Karat karat, decimal expected)
    {
        decimal actual = GoldWeight.CalculateEquivalent21KWeight(weightInGrams, karat);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CalculateEquivalent21KWeight_ZeroKaratVariantFallsThroughTo21K()
    {
        // Any karat outside the 18/24 branches is treated as 21K (weight unchanged).
        decimal actual = GoldWeight.CalculateEquivalent21KWeight(50, (Karat)0);

        Assert.Equal(50, actual);
    }
}
