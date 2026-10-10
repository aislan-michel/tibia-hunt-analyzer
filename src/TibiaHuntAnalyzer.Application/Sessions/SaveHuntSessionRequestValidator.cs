using FluentValidation;

namespace TibiaHuntAnalyzer.Application.Sessions;

public sealed class SaveHuntSessionRequestValidator : AbstractValidator<SaveHuntSessionRequest>
{
    public SaveHuntSessionRequestValidator()
    {
        RuleFor(request => request.CharacterId).NotEmpty();
        RuleFor(request => request.HuntLocationId).NotEmpty();
        RuleFor(request => request.CharacterLevel).GreaterThan(0);
        RuleFor(request => request.ParsedSession).NotNull();
        When(request => request.ParsedSession is not null, () =>
        {
            RuleFor(request => request.ParsedSession.OriginalContent).NotEmpty();
            RuleFor(request => request.ParsedSession.SessionStart).NotNull();
            RuleFor(request => request.ParsedSession.SessionEnd).NotNull();
            RuleFor(request => request.ParsedSession)
                .Must(parsed => parsed.SessionStart is null ||
                    parsed.SessionEnd is null ||
                    parsed.SessionEnd >= parsed.SessionStart)
                .WithMessage("Session end cannot be earlier than session start.");
            RuleFor(request => request.ParsedSession.Duration)
                .NotNull()
                .Must(duration => duration is null || duration >= TimeSpan.Zero);
            RuleFor(request => request.ParsedSession.KilledMonsters).NotNull();
            RuleFor(request => request.ParsedSession.LootedItems).NotNull();
        });
    }
}
