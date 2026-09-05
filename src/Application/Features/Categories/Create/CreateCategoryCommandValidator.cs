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
    }
}
