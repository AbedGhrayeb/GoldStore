// <copyright file="SetAccountBalanceCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Finance.Accounts.SetBalance;
using FluentValidation;

namespace Application.Finance.Accounts.SetBalance;

internal sealed class SetAccountBalanceCommandValidator : AbstractValidator<SetAccountBalanceCommand>
{
    public SetAccountBalanceCommandValidator()
    {
        this.RuleFor(x => x.AccountId).NotEmpty().WithMessage("معرف الحساب مطلوب");
        this.RuleFor(x => x.TargetBalance).GreaterThanOrEqualTo(0m).WithMessage("الرصيد المستهدف لا يمكن أن يكون سالباً");
        this.RuleFor(x => x.Notes).MaximumLength(500).When(x => x.Notes is not null);
    }
}
