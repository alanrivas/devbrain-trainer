using DevBrain.Domain.Entities;
using DevBrain.Domain.Enums;
using DevBrain.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DevBrain.Infrastructure.Persistence;

public sealed class EFChallengeRepository : IChallengeRepository
{
    private readonly DevBrainDbContext _context;

    public EFChallengeRepository(DevBrainDbContext context)
    {
        _context = context;
    }

    public async Task<Challenge?> GetByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            return null;

        return await _context.Challenges
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IReadOnlyList<Challenge>> GetAllAsync(ChallengeCategory? category = null, Difficulty? difficulty = null)
    {
        var query = _context.Challenges.AsQueryable();

        if (category.HasValue)
        {
            query = query.Where(c => c.Category == category.Value);
        }

        if (difficulty.HasValue)
        {
            query = query.Where(c => c.Difficulty == difficulty.Value);
        }

        var challenges = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return challenges.AsReadOnly();
    }

    public async Task AddAsync(Challenge challenge)
    {
        _context.Challenges.Add(challenge);
        await _context.SaveChangesAsync();
    }
}
