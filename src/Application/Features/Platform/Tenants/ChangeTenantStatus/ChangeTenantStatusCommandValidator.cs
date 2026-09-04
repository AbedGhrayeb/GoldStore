using Domain.Tenants;
using FluentValidation;

namespace Application.Features.Platform.Tenants.ChangeTenantStatus;

internal sealed class ChangeTenantStatusCommandValidator : AbstractValidator<ChangeTenantStatusCommand>
{
    public ChangeTenantStatusCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Status)
            .IsInEnum()
            .Must(status => status is TenantStatus.Active or TenantStatus.Suspended)
            .WithMessage("Only Active or Suspended can be set manually.");
    }
}
