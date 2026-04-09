using DevBrain.Domain.Entities;
using DevBrain.Domain.Enums;

namespace DevBrain.Domain.Interfaces;

public interface IChallengeRepository
{
    Task<Challenge?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Challenge>> GetAllAsync(ChallengeCategory? category = null, Difficulty? difficulty = null);
    Task AddAsync(Challenge challenge);
}
