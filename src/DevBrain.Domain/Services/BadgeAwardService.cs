using DevBrain.Domain.Enums;

namespace DevBrain.Domain.Services;

public sealed class BadgeAwardService : IBadgeAwardService
{
    public IReadOnlyList<BadgeType> EvaluateNewBadges(
        BadgeAwardContext context,
        IReadOnlyList<BadgeType> alreadyEarned)
    {
        var newBadges = new List<BadgeType>();

        void TryAward(BadgeType type, bool condition)
        {
            if (condition && !alreadyEarned.Contains(type))
                newBadges.Add(type);
        }

        TryAward(BadgeType.FirstBlood,    context.IsCorrect);
        TryAward(BadgeType.OnFire,        context.CurrentStreak >= 3);
        TryAward(BadgeType.WeekWarrior,   context.CurrentStreak >= 7);
        TryAward(BadgeType.RisingStar,    context.NewEloRating >= 1200);
        TryAward(BadgeType.SharpMind,     context.NewEloRating >= 1500);
        TryAward(BadgeType.Centurion,     context.TotalAttempts >= 100);
        TryAward(BadgeType.Perfectionist, context.ConsecutiveCorrect >= 10);
        TryAward(BadgeType.Brave,         context.IsCorrect && context.Difficulty == Difficulty.Hard);

        return newBadges.AsReadOnly();
    }
}
