using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevBrain.Infrastructure.Services;
using StackExchange.Redis;
using Xunit;

namespace DevBrain.Infrastructure.Tests;

public class RedisStreakServiceTests : IAsyncDisposable
{
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly IDatabase _db;
    private readonly IStreakService _sut;
    private readonly List<string> _keysToCleanup = new();

    public RedisStreakServiceTests()
    {
        _multiplexer = ConnectionMultiplexer.Connect("localhost:6379");
        _db = _multiplexer.GetDatabase();
        _sut = new RedisStreakService(_multiplexer);
    }

    public async ValueTask DisposeAsync()
    {
        if (_keysToCleanup.Count > 0)
            await _db.KeyDeleteAsync(_keysToCleanup.Select(k => (RedisKey)k).ToArray());
        _multiplexer.Dispose();
    }

    private Guid NewUser()
    {
        var userId = Guid.NewGuid();
        _keysToCleanup.Add($"streak:{userId}:count");
        _keysToCleanup.Add($"streak:{userId}:last_date");
        return userId;
    }

    private static DateTimeOffset Today(int offsetDays = 0) =>
        DateTimeOffset.UtcNow.Date.AddDays(offsetDays);

    // ─── GetStreak ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStreak_WithNoAttempts_ShouldReturnZero()
    {
        var userId = NewUser();

        var streak = await _sut.GetStreakAsync(userId);

        Assert.Equal(0, streak);
    }

    // ─── Primer attempt ────────────────────────────────────────────────────────

    [Fact]
    public async Task RecordAttempt_FirstEver_ShouldReturnOne()
    {
        var userId = NewUser();

        var streak = await _sut.RecordAttemptAsync(userId, Today());

        Assert.Equal(1, streak);
    }

    // ─── Mismo día ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task RecordAttempt_SameDayTwice_ShouldKeepStreakAtOne()
    {
        var userId = NewUser();

        await _sut.RecordAttemptAsync(userId, Today());
        var streak = await _sut.RecordAttemptAsync(userId, Today());

        Assert.Equal(1, streak);
    }

    // ─── Día consecutivo ───────────────────────────────────────────────────────

    [Fact]
    public async Task RecordAttempt_ConsecutiveDay_ShouldIncreaseStreak()
    {
        var userId = NewUser();

        await _sut.RecordAttemptAsync(userId, Today(-1));
        var streak = await _sut.RecordAttemptAsync(userId, Today());

        Assert.Equal(2, streak);
    }

    [Fact]
    public async Task RecordAttempt_ThreeConsecutiveDays_ShouldReturnThree()
    {
        var userId = NewUser();

        await _sut.RecordAttemptAsync(userId, Today(-2));
        await _sut.RecordAttemptAsync(userId, Today(-1));
        var streak = await _sut.RecordAttemptAsync(userId, Today());

        Assert.Equal(3, streak);
    }

    // ─── Streak roto ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RecordAttempt_AfterTwoDayGap_ShouldResetToOne()
    {
        var userId = NewUser();

        await _sut.RecordAttemptAsync(userId, Today(-2));
        var streak = await _sut.RecordAttemptAsync(userId, Today());

        Assert.Equal(1, streak);
    }

    [Fact]
    public async Task RecordAttempt_AfterFiveDayGap_ShouldResetToOne()
    {
        var userId = NewUser();

        await _sut.RecordAttemptAsync(userId, Today(-5));
        var streak = await _sut.RecordAttemptAsync(userId, Today());

        Assert.Equal(1, streak);
    }

    // ─── GetStreak coincide con RecordAttempt ──────────────────────────────────

    [Fact]
    public async Task GetStreak_AfterRecord_ShouldMatchReturnedValue()
    {
        var userId = NewUser();

        await _sut.RecordAttemptAsync(userId, Today(-1));
        var recorded = await _sut.RecordAttemptAsync(userId, Today());
        var fetched  = await _sut.GetStreakAsync(userId);

        Assert.Equal(recorded, fetched);
    }
}
