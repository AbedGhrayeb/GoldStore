// <copyright file="CreatePaymentCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentValidation;

namespace Application.Features.Finance.Debts.Payments;

internal sealed class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        this.RuleFor(x => x.DebtId)
            .NotEmpty()
            .WithMessage("معرف الدين مطلوب");

        this.RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("الحساب مطلوب");

        this.RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("المبلغ يجب أن يكون أكبر من صفر");

        this.RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => x.Notes is not null);

        this.RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("التاريخ مطلوب");
    }
}
