// <copyright file="UpdateExpenseCategoryCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Features.Expenses.ExpenseCategories.Update;
using FluentValidation;

namespace Application.Features.Expenses.ExpenseCategories.Update;

internal sealed class UpdateExpenseCategoryCommandValidator : AbstractValidator<UpdateExpenseCategoryCommand>
{
    public UpdateExpenseCategoryCommandValidator()
    {
        this.RuleFor(x => x.Id).NotEmpty();
        this.RuleFor(x => x.Name).NotEmpty().WithMessage("اسم التصنيف مطلوب").MaximumLength(200);
    }
}
