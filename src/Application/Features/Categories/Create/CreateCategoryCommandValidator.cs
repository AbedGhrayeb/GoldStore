using FluentValidation;

namespace Application.Categories.Create;

internal sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(500).When(c => c.Description is not null);
    }
}