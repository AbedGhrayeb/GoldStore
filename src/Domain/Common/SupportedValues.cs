namespace Domain.Common;

public static class SupportedValues
{
    public static readonly Karat[] Karats = [Karat.K18, Karat.K21, Karat.K24];

    public static readonly Currency[] Currencies = [Currency.Jod, Currency.Usd, Currency.Ils];

    public static bool IsSupported(Karat karat) => Karats.Contains(karat);

    public static bool IsSupported(Currency currency) => Currencies.Contains(currency);
}
