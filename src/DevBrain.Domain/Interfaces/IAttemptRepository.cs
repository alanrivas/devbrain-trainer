using DevBrain.Domain.Entities;

namespace DevBrain.Domain.Interfaces;

public interface IAttemptRepository
{
    Task AddAsync(Attempt attempt);
    Task<IReadOnlyList<Attempt>> GetByUserAsync(string userId);
    Task<Attempt?> GetLastByUserAsync(string userId);
    Task<int> CountCorrectByUserAsync(string userId);
}
