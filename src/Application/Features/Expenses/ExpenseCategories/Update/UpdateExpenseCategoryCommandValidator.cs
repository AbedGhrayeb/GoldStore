using Application.Features.Expenses.ExpenseCategories.Update;
using FluentValidation;

namespace Application.Features.Expenses.ExpenseCategories.Update;

internal sealed class UpdateExpenseCategoryCommandValidator : AbstractValidator<UpdateExpenseCategoryCommand>
{
    public UpdateExpenseCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().WithMessage("اسم التصنيف مطلوب").MaximumLength(200);
    }
}