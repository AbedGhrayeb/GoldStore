namespace Domain.Sales;

public enum PaymentMethod
{
    Cash = 1,
    Bank = 2
}
public static class PaymentMethodExtensions
{
    public static string ToLabel(this PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "نقدي",
        PaymentMethod.Bank => "مصرفي",
        _ => method.ToString()
    };
}
