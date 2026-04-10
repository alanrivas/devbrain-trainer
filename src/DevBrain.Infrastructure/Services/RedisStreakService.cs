using StackExchange.Redis;

namespace DevBrain.Infrastructure.Services;

public sealed class RedisStreakService : IStreakService
{
    private readonly IDatabase _db;
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(48);

    public RedisStreakService(IConnectionMultiplexer multiplexer)
    {
        _db = multiplexer.GetDatabase();
    }

    public async Task<int> RecordAttemptAsync(Guid userId, DateTimeOffset occurredAt)
    {
        var today = occurredAt.UtcDateTime.ToString("yyyy-MM-dd");
        var countKey    = $"streak:{userId}:count";
        var lastDateKey = $"streak:{userId}:last_date";

        var lastDateValue = await _db.StringGetAsync(lastDateKey);
        var countValue    = await _db.StringGetAsync(countKey);

        int count = countValue.HasValue ? (int)countValue : 0;

        if (!lastDateValue.HasValue)
        {
            // Primer attempt
            count = 1;
        }
        else
        {
            var lastDate = lastDateValue.ToString();

            if (today == lastDate)
            {
                // Mismo día — streak no cambia
                return count;
            }

            var lastDateTime = DateTime.ParseExact(lastDate, "yyyy-MM-dd", null);
            var todayDateTime = DateTime.ParseExact(today, "yyyy-MM-dd", null);
            var daysDiff = (todayDateTime - lastDateTime).Days;

            if (daysDiff == 1)
                count += 1;   // Día consecutivo
            else
                count = 1;    // Streak roto
        }

        await Task.WhenAll(
            _db.StringSetAsync(countKey,    count, Ttl),
            _db.StringSetAsync(lastDateKey, today, Ttl)
        );

        return count;
    }

    public async Task<int> GetStreakAsync(Guid userId)
    {
        var countKey = $"streak:{userId}:count";
        var value = await _db.StringGetAsync(countKey);
        return value.HasValue ? (int)value : 0;
    }
}
