// <copyright file="UpdateSubscriptionPlanCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentValidation;

namespace Application.Features.SubscriptionPlans.Update;

internal sealed class UpdateSubscriptionPlanCommandValidator : AbstractValidator<UpdateSubscriptionPlanCommand>
{
    public UpdateSubscriptionPlanCommandValidator()
    {
        this.RuleFor(x => x.Id).NotEmpty();
        this.RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        this.RuleFor(x => x.Key).MaximumLength(50)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("المفتاح يجب أن يكون أحرف صغيرة وأرقام وشرطات فقط")
            .When(x => !string.IsNullOrWhiteSpace(x.Key));

        this.RuleFor(x => x.MaximumActiveUsers)
            .GreaterThanOrEqualTo(0).When(x => x.MaximumActiveUsers.HasValue);
        this.RuleFor(x => x.MaximumPostedInvoicesPerPeriod)
            .GreaterThanOrEqualTo(0).When(x => x.MaximumPostedInvoicesPerPeriod.HasValue);
        this.RuleFor(x => x.MaximumActiveBranches)
            .GreaterThanOrEqualTo(0).When(x => x.MaximumActiveBranches.HasValue);
        this.RuleFor(x => x.MaximumStorageBytes)
            .GreaterThanOrEqualTo(0).When(x => x.MaximumStorageBytes.HasValue);

        this.RuleFor(x => x.DurationInMonths).InclusiveBetween(1, 60);
        this.RuleFor(x => x.DurationInMonths).Equal(1).When(x => x.IsTrial).WithMessage("الخطة التجريبية يجب أن تكون مدتها شهر واحد فقط.");
        this.RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        this.RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100).When(x => x.DiscountPercent.HasValue);
        this.RuleFor(x => x.Price).Equal(0).When(x => x.IsTrial).WithMessage("سعر الخطة التجريبية يجب أن يكون 0.");
        this.RuleFor(x => x.DiscountPercent).Must(v => v == null).When(x => x.IsTrial).WithMessage("الخطة التجريبية لا يمكن أن تحتوي على خصم.");
    }
}
