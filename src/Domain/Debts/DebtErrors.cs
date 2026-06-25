using SharedKernel;

namespace Domain.Debts;

public static class DebtErrors
{
    public static Error NotFound(Guid debtId) => Error.NotFound(
        "Debts.NotFound",
        $"The debt with Id = '{debtId}' was not found");

    public static Error PaymentExceedsBalance(decimal amount, decimal balance) => Error.Problem(
        "Debts.PaymentExceedsBalance",
        $"Payment amount ({amount}) exceeds remaining balance ({balance})");

    public static readonly Error PaymentAmountMustBePositive = Error.Problem(
        "Debts.PaymentAmountMustBePositive",
        "Payment amount must be greater than zero");

    public static Error AccountNotFound(Guid accountId) => Error.NotFound(
        "Debts.AccountNotFound",
        $"The financial account with Id = '{accountId}' was not found");

    public static readonly Error AccountCurrencyMismatch = Error.Problem(
        "Debts.AccountCurrencyMismatch",
        "Currency must match the linked account's currency");

    public static readonly Error NoAccountLinked = Error.Problem(
        "Debts.NoAccountLinked",
        "This debt has no linked account — cannot adjust financial balance");

    public static Error AmountBelowPayments(decimal amount, decimal payments) => Error.Problem(
        "Debts.AmountBelowPayments",
        $"New amount ({amount}) is below total payments made ({payments})");
}
