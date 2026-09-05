// <copyright file="CreateInventoryAdjustmentCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Inventory;
using FluentValidation;

namespace Application.Features.Inventory.Adjustments.Create;

internal sealed class CreateInventoryAdjustmentCommandValidator : AbstractValidator<CreateInventoryAdjustmentCommand>
{
    public CreateInventoryAdjustmentCommandValidator()
    {
        this.RuleFor(x => x.AdjustmentType)
            .Must(t => Enum.IsDefined(typeof(InventoryAdjustmentType), t))
            .WithMessage("نوع التسوية غير صالح");

        this.RuleFor(x => x.Karat)
            .Must(k => k is 18 or 21 or 24)
            .WithMessage("العيار يجب أن يكون 18 أو 21 أو 24");

        this.RuleFor(x => x.WeightInGrams)
            .GreaterThanOrEqualTo(0)
            .WithMessage("الوزن يجب أن يكون صفر أو أكثر");

        this.RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("السبب مطلوب (بحد أقصى 200 حرف)");

        this.RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null);

        this.RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("التاريخ مطلوب");
    }
}
