using Application.Features.Expenses.ExpenseCategories.Create;
using FluentValidation;

namespace Application.Features.Expenses.ExpenseCategories.Create;

internal sealed class CreateExpenseCategoryCommandValidator : AbstractValidator<CreateExpenseCategoryCommand>
{
    public CreateExpenseCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("اسم التصنيف مطلوب").MaximumLength(200);
    }
}
