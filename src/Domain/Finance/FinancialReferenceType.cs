// <copyright file="FinancialReferenceType.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Domain.Finance;

public enum FinancialReferenceType
{
    SalesPayment = 1,
    CustomerGoldPurchase = 2,
    SupplierManufacturingPayment = 3,
    Expense = 4,
    SalaryPayment = 5,
    ManualAdjustment = 6,
    DebtCreation = 7,
    DebtPayment = 8,
    DebtAdjustment = 9,
    SupplierLoan = 10,
}

public static class FinancialReferenceTypeExtensions
{
    public static string GetDescription(this FinancialReferenceType type)
    {
        return type switch
        {
            FinancialReferenceType.SalesPayment => "دفعة مبيعات",
            FinancialReferenceType.CustomerGoldPurchase => "شراء ذهب من العميل",
            FinancialReferenceType.SupplierManufacturingPayment => "دفعة تصنيع المورد",
            FinancialReferenceType.Expense => "مصروف",
            FinancialReferenceType.SalaryPayment => "دفعة راتب",
            FinancialReferenceType.ManualAdjustment => "تعديل يدوي",
            FinancialReferenceType.DebtCreation => "إنشاء دين",
            FinancialReferenceType.DebtPayment => "دفعة دين",
            FinancialReferenceType.DebtAdjustment => "تعديل دين",
            FinancialReferenceType.SupplierLoan => "قرض المورد",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }
}
