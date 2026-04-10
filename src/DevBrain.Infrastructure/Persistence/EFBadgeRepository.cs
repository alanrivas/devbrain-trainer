using DevBrain.Domain.Entities;
using DevBrain.Domain.Enums;
using DevBrain.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DevBrain.Infrastructure.Persistence;

public sealed class EFBadgeRepository : IBadgeRepository
{
    private readonly DevBrainDbContext _context;

    public EFBadgeRepository(DevBrainDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(UserBadge badge)
    {
        _context.UserBadges.Add(badge);
        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<UserBadge>> GetByUserAsync(Guid userId)
    {
        return await _context.UserBadges
            .Where(b => b.UserId == userId)
            .OrderBy(b => b.EarnedAt)
            .ToListAsync();
    }

    public async Task<bool> HasBadgeAsync(Guid userId, BadgeType type)
    {
        return await _context.UserBadges
            .AnyAsync(b => b.UserId == userId && b.Type == type);
    }
}
