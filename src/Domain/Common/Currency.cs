namespace Domain.Common;

public enum Currency
{
    JOD = 1,
    USD = 2,
    ILS = 3
}
public static class CurrencyExtensions
{
    public static string ToCurrencyString(this Currency currency)
    {
        return currency switch
        {
            Currency.JOD => "JOD",
            Currency.USD => "USD",
            Currency.ILS => "ILS",
            _ => currency.ToString()
        };
    }
    public static readonly Dictionary<Currency, (string Code, string Symbol)> CurrencyLabels = new()
    {
        [Currency.JOD] = ("Jod", "د.أ"),
        [Currency.USD] = ("Usd", "$"),
        [Currency.ILS] = ("Ils", "₪")
    };
}
