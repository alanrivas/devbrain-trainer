using DevBrain.Domain.Enums;

namespace DevBrain.Domain.Services;

public interface IBadgeAwardService
{
    IReadOnlyList<BadgeType> EvaluateNewBadges(
        BadgeAwardContext context,
        IReadOnlyList<BadgeType> alreadyEarned);
}
