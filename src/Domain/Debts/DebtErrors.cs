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
}
