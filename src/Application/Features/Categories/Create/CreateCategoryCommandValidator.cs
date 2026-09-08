// <copyright file="CreateCategoryCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentValidation;

namespace Application.Categories.Create;

internal sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        this.RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        this.RuleFor(c => c.Description).MaximumLength(500).When(c => c.Description is not null);
        this.RuleFor(c => c.WeightInGrams).GreaterThan(0).WithMessage("وزن الفئة مطلوب ويجب أن يكون أكبر من صفر");
        this.RuleFor(c => c.Karat).Must(k => k is 18 or 21 or 24).WithMessage("عيار الفئة مطلوب");
    }
}
