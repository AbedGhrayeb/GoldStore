// <copyright file="SupplierFinancialTransactionDirection.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Domain.Suppliers;

public enum SupplierFinancialTransactionDirection
{
    FromSupplier = 1,
    ToSupplier = 2,
}

public static class SupplierFinancialTransactionDirectionExtentions
{
    public static string ToCurrencyString(this SupplierFinancialTransactionDirection direction)
    {
        return direction switch
        {
            SupplierFinancialTransactionDirection.FromSupplier => "سلفة من مورد",
            SupplierFinancialTransactionDirection.ToSupplier => "سلفة لمورد",
            _ => "معاملة مالية",
        };
    }
}
