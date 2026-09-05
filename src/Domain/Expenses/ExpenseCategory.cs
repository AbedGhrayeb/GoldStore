// <copyright file="ExpenseCategory.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using SharedKernel;
using SharedKernel.Result;

namespace Domain.Expenses;

public sealed class ExpenseCategory : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    public string Name { get; private set; }

    private readonly List<Expense> expenses = new();

    public IReadOnlyCollection<Expense> Expenses => this.expenses.AsReadOnly();

    private ExpenseCategory()
    {
    }

    private ExpenseCategory(string name)
    {
        this.Name = name;
    }

    public static Result<ExpenseCategory> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ExpenseCategoryErrors.NameRequired;
        }

        return new ExpenseCategory(name);
    }

    public Result<Updated> Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ExpenseCategoryErrors.NameRequired;
        }

        this.Name = name;
        return Result.Updated;
    }
}
