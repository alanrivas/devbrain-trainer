using DevBrain.Domain.Entities;
using DevBrain.Domain.Enums;
using DevBrain.Domain.Exceptions;
using DevBrain.Domain.Services;

namespace DevBrain.Domain.Tests;

public class BadgeAwardServiceTests
{
    private readonly IBadgeAwardService _sut = new BadgeAwardService();

    private static BadgeAwardContext DefaultContext(
        bool isCorrect = true,
        Difficulty difficulty = Difficulty.Easy,
        int totalAttempts = 1,
        int consecutiveCorrect = 1,
        int currentStreak = 1,
        int newEloRating = 1000) =>
        new(isCorrect, difficulty, totalAttempts, consecutiveCorrect, currentStreak, newEloRating);

    // ─── UserBadge.Create ─────────────────────────────────────────────────────

    [Fact]
    public void Create_GivenValidArguments_ShouldReturnUserBadge()
    {
        var userId = Guid.NewGuid();
        var badge = UserBadge.Create(userId, BadgeType.FirstBlood);

        Assert.NotNull(badge);
        Assert.NotEqual(Guid.Empty, badge.Id);
        Assert.Equal(userId, badge.UserId);
        Assert.Equal(BadgeType.FirstBlood, badge.Type);
    }

    [Fact]
    public void Create_GivenEmptyUserId_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => UserBadge.Create(Guid.Empty, BadgeType.FirstBlood));
    }

    [Fact]
    public void Create_EarnedAt_ShouldBeUtc()
    {
        var badge = UserBadge.Create(Guid.NewGuid(), BadgeType.Brave);

        Assert.Equal(TimeSpan.Zero, badge.EarnedAt.Offset);
    }

    // ─── FirstBlood ───────────────────────────────────────────────────────────

    [Fact]
    public void EvaluateNewBadges_FirstBlood_GivenCorrectAnswer_ShouldAward()
    {
        var result = _sut.EvaluateNewBadges(DefaultContext(isCorrect: true), []);

        Assert.Contains(BadgeType.FirstBlood, result);
    }

    [Fact]
    public void EvaluateNewBadges_FirstBlood_GivenIncorrectAnswer_ShouldNotAward()
    {
        var result = _sut.EvaluateNewBadges(DefaultContext(isCorrect: false), []);

        Assert.DoesNotContain(BadgeType.FirstBlood, result);
    }

    [Fact]
    public void EvaluateNewBadges_FirstBlood_GivenAlreadyEarned_ShouldNotAward()
    {
        var result = _sut.EvaluateNewBadges(DefaultContext(isCorrect: true), [BadgeType.FirstBlood]);

        Assert.DoesNotContain(BadgeType.FirstBlood, result);
    }

    // ─── OnFire / WeekWarrior ─────────────────────────────────────────────────

    [Fact]
    public void EvaluateNewBadges_OnFire_GivenStreak2_ShouldNotAwardEither()
    {
        var result = _sut.EvaluateNewBadges(DefaultContext(currentStreak: 2), []);

        Assert.DoesNotContain(BadgeType.OnFire, result);
        Assert.DoesNotContain(BadgeType.WeekWarrior, result);
    }

    [Fact]
    public void EvaluateNewBadges_OnFire_GivenStreak3_ShouldAwardOnFireOnly()
    {
        var result = _sut.EvaluateNewBadges(DefaultContext(currentStreak: 3), []);

        Assert.Contains(BadgeType.OnFire, result);
        Assert.DoesNotContain(BadgeType.WeekWarrior, result);
    }

    [Fact]
    public void EvaluateNewBadges_WeekWarrior_GivenStreak7_ShouldAwardBoth()
    {
        var result = _sut.EvaluateNewBadges(DefaultContext(currentStreak: 7), []);

        Assert.Contains(BadgeType.OnFire, result);
        Assert.Contains(BadgeType.WeekWarrior, result);
    }

    [Fact]
    public void EvaluateNewBadges_WeekWarrior_GivenStreak7AndBothAlreadyEarned_ShouldAwardNone()
    {
        var result = _sut.EvaluateNewBadges(DefaultContext(currentStreak: 7), [BadgeType.OnFire, BadgeType.WeekWarrior]);

        Assert.DoesNotContain(BadgeType.OnFire, result);
        Assert.DoesNotContain(BadgeType.WeekWarrior, result);
    }

    [Fact]
    public void EvaluateNewBadges_WeekWarrior_GivenStreak7AndOnlyOnFireEarned_ShouldAwardWeekWarriorOnly()
    {
        var result = _sut.EvaluateNewBadges(DefaultContext(currentStreak: 7), [BadgeType.OnFire]);

        Assert.DoesNotContain(BadgeType.OnFire, result);
        Assert.Contains(BadgeType.WeekWarrior, result);
    }

    // ─── RisingStar / SharpMind ───────────────────────────────────────────────

    [Fact]
    public void EvaluateNewBadges_RisingStar_GivenElo1199_ShouldNotAwardEither()
    {
        var result = _sut.EvaluateNewBadges(DefaultContext(newEloRating: 1199), []);

        Assert.DoesNotContain(BadgeType.RisingStar, result);
        Assert.DoesNotContain(BadgeType.SharpMind, result);
    }

    [Fact]
    public void EvaluateNewBadges_RisingStar_GivenElo1200_ShouldAwardRisingStarOnly()
    {
        var result = _sut.EvaluateNewBadges(DefaultContext(newEloRating: 1200), []);

        Assert.Contains(BadgeType.RisingStar, result);
        Assert.DoesNotContain(BadgeType.SharpMind, result);
    }

    [Fact]
    public void EvaluateNewBadges_SharpMind_GivenElo1500_ShouldAwardBoth()
    {
        var result = _sut.EvaluateNewBadges(DefaultContext(newEloRating: 1500), []);

        Assert.Contains(BadgeType.RisingStar, result);
        Assert.Contains(BadgeType.SharpMind, result);
    }

    [Fact]
    public void EvaluateNewBadges_SharpMind_GivenElo1500AndRisingStarEarned_ShouldAwardSharpMindOnly()
    {
        var result = _sut.EvaluateNewBadges(DefaultContext(newEloRating: 1500), [BadgeType.RisingStar]);

        Assert.DoesNotContain(BadgeType.RisingStar, result);
        Assert.Contains(BadgeType.SharpMind, result);
    }

    // ─── Centurion ────────────────────────────────────────────────────────────

    [Fact]
    public void EvaluateNewBadges_Centurion_GivenTotalAttempts99_ShouldNotAward()
    {
        var result = _sut.EvaluateNewBadges(DefaultContext(totalAttempts: 99), []);

        Assert.DoesNotContain(BadgeType.Centurion, result);
    }

    [Fact]
    public void EvaluateNewBadges_Centurion_GivenTotalAttempts100_ShouldAward()
    {
        var result = _sut.EvaluateNewBadges(DefaultContext(totalAttempts: 100), []);

        Assert.Contains(BadgeType.Centurion, result);
    }

    [Fact]
    public void EvaluateNewBadges_Centurion_GivenAlreadyEarned_ShouldNotAward()
    {
        var result = _sut.EvaluateNewBadges(DefaultContext(totalAttempts: 150), [BadgeType.Centurion]);

        Assert.DoesNotContain(BadgeType.Centurion, result);
    }

    // ─── Perfectionist ────────────────────────────────────────────────────────

    [Fact]
    public void EvaluateNewBadges_Perfectionist_GivenConsecutiveCorrect9_ShouldNotAward()
    {
        var result = _sut.EvaluateNewBadges(DefaultContext(consecutiveCorrect: 9), []);

        Assert.DoesNotContain(BadgeType.Perfectionist, result);
    }

    [Fact]
    public void EvaluateNewBadges_Perfectionist_GivenConsecutiveCorrect10_ShouldAward()
    {
        var result = _sut.EvaluateNewBadges(DefaultContext(consecutiveCorrect: 10), []);

        Assert.Contains(BadgeType.Perfectionist, result);
    }

    [Fact]
    public void EvaluateNewBadges_Perfectionist_GivenAlreadyEarned_ShouldNotAward()
    {
        var result = _sut.EvaluateNewBadges(DefaultContext(consecutiveCorrect: 15), [BadgeType.Perfectionist]);

        Assert.DoesNotContain(BadgeType.Perfectionist, result);
    }

    // ─── Brave ────────────────────────────────────────────────────────────────

    [Fact]
    public void EvaluateNewBadges_Brave_GivenCorrectHard_ShouldAward()
    {
        var result = _sut.EvaluateNewBadges(DefaultContext(isCorrect: true, difficulty: Difficulty.Hard), []);

        Assert.Contains(BadgeType.Brave, result);
    }

    [Fact]
    public void EvaluateNewBadges_Brave_GivenIncorrectHard_ShouldNotAward()
    {
        var result = _sut.EvaluateNewBadges(DefaultContext(isCorrect: false, difficulty: Difficulty.Hard, consecutiveCorrect: 0), []);

        Assert.DoesNotContain(BadgeType.Brave, result);
    }

    [Fact]
    public void EvaluateNewBadges_Brave_GivenCorrectMedium_ShouldNotAward()
    {
        var result = _sut.EvaluateNewBadges(DefaultContext(isCorrect: true, difficulty: Difficulty.Medium), []);

        Assert.DoesNotContain(BadgeType.Brave, result);
    }

    [Fact]
    public void EvaluateNewBadges_Brave_GivenAlreadyEarned_ShouldNotAward()
    {
        var result = _sut.EvaluateNewBadges(DefaultContext(isCorrect: true, difficulty: Difficulty.Hard), [BadgeType.Brave]);

        Assert.DoesNotContain(BadgeType.Brave, result);
    }

    // ─── Múltiples badges simultáneos ─────────────────────────────────────────

    [Fact]
    public void EvaluateNewBadges_MultipleBadges_GivenSeveralConditionsMet_ShouldAwardAll()
    {
        var ctx = new BadgeAwardContext(
            IsCorrect: true,
            Difficulty: Difficulty.Easy,
            TotalAttempts: 1,
            ConsecutiveCorrect: 1,
            CurrentStreak: 3,
            NewEloRating: 1200);

        var result = _sut.EvaluateNewBadges(ctx, []);

        Assert.Contains(BadgeType.FirstBlood, result);
        Assert.Contains(BadgeType.OnFire, result);
        Assert.Contains(BadgeType.RisingStar, result);
    }

    [Fact]
    public void EvaluateNewBadges_AllAlreadyEarned_GivenAllConditionsMet_ShouldReturnEmpty()
    {
        var ctx = new BadgeAwardContext(
            IsCorrect: true,
            Difficulty: Difficulty.Hard,
            TotalAttempts: 100,
            ConsecutiveCorrect: 10,
            CurrentStreak: 7,
            NewEloRating: 1500);

        var allBadges = Enum.GetValues<BadgeType>().ToList();
        var result = _sut.EvaluateNewBadges(ctx, allBadges);

        Assert.Empty(result);
    }
}
