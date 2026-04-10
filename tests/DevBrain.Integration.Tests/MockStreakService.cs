using DevBrain.Infrastructure.Services;

namespace DevBrain.Integration.Tests;

/// <summary>
/// Mock StreakService for integration tests — records to in-memory list instead of Redis
/// </summary>
public class MockStreakService : IStreakService
{
    private readonly Dictionary<Guid, (DateTimeOffset lastAttempt, int streak)> _streaks = new();

    public async Task<int> GetStreakAsync(Guid userId)
    {
        await Task.Delay(0); // Simulate async
        return _streaks.TryGetValue(userId, out var data) ? data.streak : 0;
    }

    public async Task<int> RecordAttemptAsync(Guid userId, DateTimeOffset attemptTime)
    {
        await Task.Delay(0); // Simulate async

        if (!_streaks.TryGetValue(userId, out var previousData))
        {
            _streaks[userId] = (attemptTime, 1);
            return 1;
        }

        var (lastAttempt, streak) = previousData;
        var daysSinceLastAttempt = (attemptTime.Date - lastAttempt.Date).Days;

        int newStreak = daysSinceLastAttempt switch
        {
            0 => streak, // Same day, keep streak
            1 => streak + 1, // Next day, increment
            _ => 1 // Gap > 1 day, reset
        };

        _streaks[userId] = (attemptTime, newStreak);
        return newStreak;
    }

    public async Task ResetStreakAsync(Guid userId)
    {
        await Task.Delay(0); // Simulate async
        _streaks.Remove(userId);
    }
}
