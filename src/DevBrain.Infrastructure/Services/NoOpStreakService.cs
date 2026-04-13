namespace DevBrain.Infrastructure.Services;

/// <summary>
/// No-operation Streak Service — Used when Redis is not available.
/// Implements IStreakService but doesn't persist anything.
/// </summary>
public sealed class NoOpStreakService : IStreakService
{
    public async Task<int> RecordAttemptAsync(Guid userId, DateTimeOffset occurredAt)
    {
        // Return 1 (treated as a new attempt recorded, but not persisted)
        await Task.CompletedTask;
        return 1;
    }

    public async Task<int> GetStreakAsync(Guid userId)
    {
        // Always return 0 (no streak tracked)
        await Task.CompletedTask;
        return 0;
    }
}
