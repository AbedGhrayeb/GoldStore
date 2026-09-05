// <copyright file="CreateExpenseCategoryCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Features.Expenses.ExpenseCategories.Create;
using FluentValidation;

namespace Application.Features.Expenses.ExpenseCategories.Create;

internal sealed class CreateExpenseCategoryCommandValidator : AbstractValidator<CreateExpenseCategoryCommand>
{
    public CreateExpenseCategoryCommandValidator()
    {
        this.RuleFor(x => x.Name).NotEmpty().WithMessage("اسم التصنيف مطلوب").MaximumLength(200);
    }
}
