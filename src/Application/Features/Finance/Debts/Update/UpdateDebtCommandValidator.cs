// <copyright file="UpdateDebtCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentValidation;

namespace Application.Features.Finance.Debts.Update;

internal sealed class UpdateDebtCommandValidator : AbstractValidator<UpdateDebtCommand>
{
    public UpdateDebtCommandValidator()
    {
        this.RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("المعرف مطلوب");

        this.RuleFor(x => x.Name)
            .MaximumLength(200)
            .When(x => x.Name is not null);

        this.RuleFor(x => x.Phone)
            .MaximumLength(10)
            .When(x => x.Phone is not null);

        this.RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => x.Notes is not null);

        this.RuleFor(x => x.NewAmount)
            .GreaterThan(0)
            .When(x => x.NewAmount.HasValue)
            .WithMessage("المبلغ الجديد يجب أن يكون أكبر من صفر");

        this.RuleFor(x => x.NewAccountId)
            .NotEmpty()
            .When(x => x.NewAccountId.HasValue)
            .WithMessage("الحساب المالي الجديد غير صالح");
    }
}
