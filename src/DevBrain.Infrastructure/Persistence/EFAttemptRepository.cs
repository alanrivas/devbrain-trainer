using DevBrain.Domain.Entities;
using DevBrain.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DevBrain.Infrastructure.Persistence;

public sealed class EFAttemptRepository : IAttemptRepository
{
    private readonly DevBrainDbContext _context;

    public EFAttemptRepository(DevBrainDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Attempt attempt)
    {
        _context.Attempts.Add(attempt);
        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Attempt>> GetByUserAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new List<Attempt>().AsReadOnly();

        var attempts = await _context.Attempts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.OccurredAt)
            .ToListAsync();

        return attempts.AsReadOnly();
    }

    public async Task<Attempt?> GetLastByUserAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        return await _context.Attempts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.OccurredAt)
            .FirstOrDefaultAsync();
    }

    public async Task<int> CountCorrectByUserAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return 0;

        return await _context.Attempts
            .Where(a => a.UserId == userId && a.IsCorrect)
            .CountAsync();
    }
}
