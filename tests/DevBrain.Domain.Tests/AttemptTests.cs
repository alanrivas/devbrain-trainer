using DevBrain.Domain.Entities;
using DevBrain.Domain.Enums;
using DevBrain.Domain.Exceptions;

namespace DevBrain.Domain.Tests;

public class AttemptTests
{
    // --- Helpers ---

    private static Challenge CreateChallenge() => Challenge.Create(
        title: "What does SELECT * do?",
        description: "Explain what SELECT * does in SQL.",
        category: ChallengeCategory.Sql,
        difficulty: Difficulty.Easy,
        correctAnswer: "selects all columns",
        timeLimitSecs: 60
    );

    private static readonly Guid TestUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

    private static Attempt CreateValid(Challenge challenge) =>
        Attempt.Create(challenge.Id, TestUserId, "selects all columns", elapsedSecs: 30, challenge);

    // --- Creación válida ---

    [Fact]
    public void Create_GivenCorrectAnswer_ShouldReturnAttemptWithIsCorrectTrue()
    {
        var challenge = CreateChallenge();

        var attempt = CreateValid(challenge);

        Assert.True(attempt.IsCorrect);
    }

    [Fact]
    public void Create_GivenWrongAnswer_ShouldReturnAttemptWithIsCorrectFalse()
    {
        var challenge = CreateChallenge();

        var attempt = Attempt.Create(challenge.Id, TestUserId, "drops the table", elapsedSecs: 30, challenge);

        Assert.False(attempt.IsCorrect);
    }

    [Fact]
    public void Create_GivenValidArguments_ShouldAssignIdAndOccurredAt()
    {
        var challenge = CreateChallenge();

        var attempt = CreateValid(challenge);

        Assert.NotEqual(Guid.Empty, attempt.Id);
        Assert.NotEqual(default, attempt.OccurredAt);
    }

    [Fact]
    public void Create_GivenValidArguments_ShouldStoreUserId()
    {
        var challenge = CreateChallenge();

        var attempt = Attempt.Create(challenge.Id, TestUserId, "selects all columns", elapsedSecs: 30, challenge);

        Assert.Equal(TestUserId, attempt.UserId);
    }

    // --- Validaciones ---

    [Fact]
    public void Create_GivenEmptyChallengeId_ShouldThrowDomainException()
    {
        var challenge = CreateChallenge();

        Assert.Throws<DomainException>(() =>
            Attempt.Create(Guid.Empty, TestUserId, "selects all columns", elapsedSecs: 30, challenge));
    }

    [Fact]
    public void Create_GivenEmptyUserId_ShouldThrowDomainException()
    {
        var challenge = CreateChallenge();

        Assert.Throws<DomainException>(() =>
            Attempt.Create(challenge.Id, Guid.Empty, "selects all columns", elapsedSecs: 30, challenge));
    }

    [Fact]
    public void Create_GivenEmptyUserAnswer_ShouldThrowDomainException()
    {
        var challenge = CreateChallenge();

        Assert.Throws<DomainException>(() =>
            Attempt.Create(challenge.Id, TestUserId, "", elapsedSecs: 30, challenge));
    }

    [Fact]
    public void Create_GivenElapsedSecsZero_ShouldSucceed()
    {
        // Spec allows >= 0, so zero is valid (though unusual)
        var challenge = CreateChallenge();

        var attempt = Attempt.Create(challenge.Id, TestUserId, "selects all columns", elapsedSecs: 0, challenge);

        Assert.NotNull(attempt);
        Assert.Equal(0, attempt.ElapsedSecs);
    }

    [Fact]
    public void Create_GivenElapsedSecsExceedsTimeLimit_ShouldSucceedWithWarning()
    {
        // Spec allows exceeding time limit - user can try even after time expires
        var challenge = CreateChallenge(); // timeLimitSecs = 60

        var attempt = Attempt.Create(challenge.Id, TestUserId, "selects all columns", elapsedSecs: 61, challenge);

        Assert.NotNull(attempt);
        Assert.Equal(61, attempt.ElapsedSecs);
        // In production, frontend would show a warning, but attempt is still valid
    }
}
