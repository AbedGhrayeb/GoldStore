using FluentValidation;

namespace Application.Categories.Update;

internal sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(500).When(c => c.Description is not null);
    }
}