using FluentValidation;

namespace Application.Features.SubscriptionPlans.Update;

internal sealed class UpdateSubscriptionPlanCommandValidator : AbstractValidator<UpdateSubscriptionPlanCommand>
{
    public UpdateSubscriptionPlanCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Key).MaximumLength(50)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("المفتاح يجب أن يكون أحرف صغيرة وأرقام وشرطات فقط")
            .When(x => !string.IsNullOrWhiteSpace(x.Key));

        RuleFor(x => x.MaximumActiveUsers)
            .GreaterThanOrEqualTo(0).When(x => x.MaximumActiveUsers.HasValue);
        RuleFor(x => x.MaximumPostedInvoicesPerPeriod)
            .GreaterThanOrEqualTo(0).When(x => x.MaximumPostedInvoicesPerPeriod.HasValue);
        RuleFor(x => x.MaximumActiveBranches)
            .GreaterThanOrEqualTo(0).When(x => x.MaximumActiveBranches.HasValue);
        RuleFor(x => x.MaximumStorageBytes)
            .GreaterThanOrEqualTo(0).When(x => x.MaximumStorageBytes.HasValue);

        RuleFor(x => x.DurationInMonths).InclusiveBetween(1, 60);
        RuleFor(x => x.DurationInMonths).Equal(1).When(x => x.IsTrial).WithMessage("الخطة التجريبية يجب أن تكون مدتها شهر واحد فقط.");
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100).When(x => x.DiscountPercent.HasValue);
        RuleFor(x => x.Price).Equal(0).When(x => x.IsTrial).WithMessage("سعر الخطة التجريبية يجب أن يكون 0.");
        RuleFor(x => x.DiscountPercent).Must(v => v == null).When(x => x.IsTrial).WithMessage("الخطة التجريبية لا يمكن أن تحتوي على خصم.");
    }
}
