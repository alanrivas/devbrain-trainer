using DevBrain.Domain.Enums;
using DevBrain.Domain.Exceptions;

namespace DevBrain.Domain.Entities;

public sealed class UserBadge
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public BadgeType Type { get; }
    public DateTimeOffset EarnedAt { get; }

    private UserBadge(Guid id, Guid userId, BadgeType type, DateTimeOffset earnedAt)
    {
        Id = id;
        UserId = userId;
        Type = type;
        EarnedAt = earnedAt;
    }

    public static UserBadge Create(Guid userId, BadgeType type)
    {
        if (userId == Guid.Empty)
            throw new DomainException("UserId is required.");

        return new UserBadge(
            id: Guid.NewGuid(),
            userId: userId,
            type: type,
            earnedAt: DateTimeOffset.UtcNow);
    }
}
