using System.Text.Json;
using FluentValidation;

namespace Application.Features.TenantSettings.UpdateTenantSettings;

internal sealed class UpdateTenantSettingsCommandValidator : AbstractValidator<UpdateTenantSettingsCommand>
{
    private const int MaxSettingsCharacters = 16 * 1024;

    public UpdateTenantSettingsCommandValidator()
    {
        RuleFor(c => c.Settings)
            .Must(s => s.ValueKind == JsonValueKind.Object)
            .WithMessage("الإعدادات يجب أن تكون كائن JSON صالح.");

        RuleFor(c => c.Settings.GetRawText().Length)
            .LessThanOrEqualTo(MaxSettingsCharacters)
            .WithMessage($"حجم الإعدادات يتجاوز الحد الأقصى المسموح ({MaxSettingsCharacters} حرف).");
    }
}
