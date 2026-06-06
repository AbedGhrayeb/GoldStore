namespace Domain.Common;

public sealed record GoldWeight
{
    private GoldWeight(decimal weightInGrams, Karat karat)
    {
        WeightInGrams = weightInGrams;
        Karat = karat;
        Equivalent21KWeightInGrams = CalculateEquivalent21KWeight(weightInGrams, karat);
    }

    public decimal WeightInGrams { get; }

    public Karat Karat { get; }

    public decimal Equivalent21KWeightInGrams { get; }

    public static GoldWeight Create(decimal weightInGrams, Karat karat)
    {
        if (weightInGrams <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weightInGrams), "Gold weight must be greater than zero.");
        }

        return new GoldWeight(weightInGrams, karat);
    }

    public static decimal CalculateEquivalent21KWeight(decimal weightInGrams, Karat karat) =>
        weightInGrams * (decimal)karat / (decimal)Karat.K21;
}
