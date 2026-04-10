using DevBrain.Domain.Entities;
using DevBrain.Domain.Enums;

namespace DevBrain.Domain.Interfaces;

public interface IBadgeRepository
{
    Task AddAsync(UserBadge badge);
    Task<IReadOnlyList<UserBadge>> GetByUserAsync(Guid userId);
    Task<bool> HasBadgeAsync(Guid userId, BadgeType type);
}
