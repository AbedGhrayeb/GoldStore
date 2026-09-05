// <copyright file="InvenToryAdjustmentErrors.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using SharedKernel.Result;

namespace Domain.Inventory;

public static class InvenToryAdjustmentErrors
{
    public static Error ReasonRequired => Error.Validation(
          "Debts.ReasonRequired",
          "سبب التعديل يجب أن يكون محددًا.");

    public static Error WeightMustbeGreaterThanZero => Error.Validation(
          "Debts.WeightMustbeGreaterThanZero",
          "وزن الذهب يجب أن يكون أكبر من الصفر.");
}
