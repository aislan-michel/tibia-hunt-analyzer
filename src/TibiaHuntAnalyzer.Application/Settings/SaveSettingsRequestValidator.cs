using FluentValidation;

namespace TibiaHuntAnalyzer.Application.Settings;

public sealed class SaveSettingsRequestValidator : AbstractValidator<SaveSettingsRequest>
{
    public SaveSettingsRequestValidator()
    {
        RuleFor(request => request.Characters).NotNull();
        RuleFor(request => request.HuntLocations).NotNull();
        RuleForEach(request => request.Characters).SetValidator(new SettingItemValidator());
        RuleForEach(request => request.HuntLocations).SetValidator(new SettingItemValidator());
    }

    private sealed class SettingItemValidator : AbstractValidator<SettingItem>
    {
        public SettingItemValidator()
        {
            RuleFor(item => item.Name)
                .NotEmpty()
                .WithMessage("A name is required.")
                .MaximumLength(200)
                .WithMessage("A name cannot exceed 200 characters.");
        }
    }
}
