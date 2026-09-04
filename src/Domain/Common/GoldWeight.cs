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
            throw new ArgumentOutOfRangeException(nameof(weightInGrams), "وزن الذهب يجب أن يكون أكبر من الصفر.");
        }

        return new GoldWeight(weightInGrams, karat);
    }

    public static decimal CalculateEquivalent21KWeight(decimal weightInGrams, Karat karat)
    {
        return Math.Round(karat == Karat.K24 ? weightInGrams / 875 * 1000 : karat == Karat.K18 ? weightInGrams * 700 / 875 : weightInGrams, 3);
    }

    //weightInGrams * (decimal)karat / (decimal)Karat.K21;
}
