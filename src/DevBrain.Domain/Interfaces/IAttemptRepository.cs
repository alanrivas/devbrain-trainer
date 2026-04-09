using DevBrain.Domain.Entities;

namespace DevBrain.Domain.Interfaces;

public interface IAttemptRepository
{
    Task AddAsync(Attempt attempt);
    Task<IReadOnlyList<Attempt>> GetByUserAsync(Guid userId);
    Task<Attempt?> GetLastByUserAsync(Guid userId);
    Task<int> CountCorrectByUserAsync(Guid userId);
}
