using Domain.Inventory;
using FluentValidation;

namespace Application.Features.Inventory.Adjustments.Create;

internal sealed class CreateInventoryAdjustmentCommandValidator : AbstractValidator<CreateInventoryAdjustmentCommand>
{
    public CreateInventoryAdjustmentCommandValidator()
    {
        RuleFor(x => x.AdjustmentType)
            .Must(t => Enum.IsDefined(typeof(InventoryAdjustmentType), t))
            .WithMessage("نوع التسوية غير صالح");

        RuleFor(x => x.Karat)
            .Must(k => k is 18 or 21 or 24)
            .WithMessage("العيار يجب أن يكون 18 أو 21 أو 24");

        RuleFor(x => x.WeightInGrams)
            .GreaterThanOrEqualTo(0)
            .WithMessage("الوزن يجب أن يكون صفر أو أكثر");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("السبب مطلوب (بحد أقصى 200 حرف)");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null);

        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("التاريخ مطلوب");
    }
}