using SharedKernel;

namespace Domain.Finance;

public static class FinancialAccountErrors
{
    public static Error NotFound(Guid accountId) => Error.NotFound(
        "Finance.AccountNotFound",
        $"The financial account with Id = '{accountId}' was not found");

    public static readonly Error Inactive = Error.Conflict(
        "Finance.AccountInactive",
        "Cannot adjust the balance of an inactive financial account");

    public static readonly Error InvalidTargetBalance = Error.Problem(
        "Finance.InvalidTargetBalance",
        "Target balance cannot be negative");
}
