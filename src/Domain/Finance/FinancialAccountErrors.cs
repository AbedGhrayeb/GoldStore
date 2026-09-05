// <copyright file="FinancialAccountErrors.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using SharedKernel;
using SharedKernel.Result;

namespace Domain.Finance;

public static class FinancialAccountErrors
{
    public static Error AccountIdRequired => Error.Validation(
        "Finance.AccountIdRequired",
        "رقم الحساب مطلوب");

    public static Error AccountNameRequired => Error.Validation(
        "Finance.AccountNameRequired",
        "اسم الحساب مطلوب");

    public static Error ReferenceId => Error.Validation(
        "Finance.ReferenceIdRequired",
        "رقم المرجع مطلوب");

    public static Error NotFound(Guid accountId) => Error.NotFound(
        "Finance.AccountNotFound",
        $"The financial account with Id = '{accountId}' was not found");

    public static readonly Error Inactive = Error.Conflict(
        "Finance.AccountInactive", "الحساب غير مفعل");

    public static readonly Error InvalidTargetBalance = Error.Validation(
        "Finance.InvalidTargetBalance",
        "Target balance cannot be negative");

    public static Error InsufficientBalance(decimal available, decimal required) => Error.Conflict(
        "Finance.InsufficientBalance",
        $"الرصيد غير كافٍ في الحساب (الرصيد المتاح: {available:N2}، المطلوب: {required:N2})");
}
