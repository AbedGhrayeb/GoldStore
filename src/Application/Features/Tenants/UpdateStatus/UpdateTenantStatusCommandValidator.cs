using FluentValidation;

namespace Application.Tenants.UpdateStatus;

internal sealed class UpdateTenantStatusCommandValidator : AbstractValidator<UpdateTenantStatusCommand>
{
    public UpdateTenantStatusCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.NewStatus).IsInEnum();
    }
}
