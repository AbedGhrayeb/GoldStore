// <copyright file="PaymentErrors.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using SharedKernel;
using SharedKernel.Result;

namespace Domain.Finance;

public static class PaymentErrors
{
    public static Error LegAmountMustBePositive => Error.Validation(
        "Finance.PaymentLegAmountPositive",
        "مبلغ الدفعة يجب أن يكون أكبر من صفر");

    public static Error ExchangeRateMustBePositive => Error.Validation(
        "Finance.ExchangeRatePositive",
        "سعر الصرف يجب أن يكون أكبر من صفر");

    public static Error LegAccountCurrencyMismatch(string accountCurrency, string legCurrency) => Error.Conflict(
        "Finance.PaymentLegCurrencyMismatch",
        $"عملة الحساب ({accountCurrency}) لا تطابق عملة الدفعة ({legCurrency})");

    public static Error LegAccountNotFound => Error.NotFound(
        "Finance.PaymentLegAccountNotFound",
        "حساب الدفعة غير موجود");

    public static Error LegAccountInactive => Error.Conflict(
        "Finance.PaymentLegAccountInactive",
        "حساب الدفعة غير مفعل");

    public static Error LegsExceedTotal(decimal available, decimal required) => Error.Validation(
        "Finance.PaymentLegsExceedTotal",
        $"إجمالي الدفعات يتجاوز المبلغ المستحق (المتاح: {available:N3}، المطلوب: {required:N3})");
}
