// <copyright file="UpdateCategoryCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentValidation;

namespace Application.Categories.Update;

internal sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        this.RuleFor(c => c.Id).NotEmpty();
        this.RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        this.RuleFor(c => c.Description).MaximumLength(500).When(c => c.Description is not null);
    }
}
