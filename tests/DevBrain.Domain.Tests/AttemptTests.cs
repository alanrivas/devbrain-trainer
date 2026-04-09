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

    // --- Creación válida ---

    [Fact]
    public void Create_GivenCorrectAnswer_ShouldReturnAttemptWithIsCorrectTrue()
    {
        var challenge = CreateChallenge();

        var attempt = Attempt.Create(challenge.Id, "selects all columns", elapsedSecs: 30, challenge);

        Assert.True(attempt.IsCorrect);
    }

    [Fact]
    public void Create_GivenWrongAnswer_ShouldReturnAttemptWithIsCorrectFalse()
    {
        var challenge = CreateChallenge();

        var attempt = Attempt.Create(challenge.Id, "drops the table", elapsedSecs: 30, challenge);

        Assert.False(attempt.IsCorrect);
    }

    [Fact]
    public void Create_GivenValidArguments_ShouldAssignIdAndOccurredAt()
    {
        var challenge = CreateChallenge();

        var attempt = Attempt.Create(challenge.Id, "selects all columns", elapsedSecs: 30, challenge);

        Assert.NotEqual(Guid.Empty, attempt.Id);
        Assert.NotEqual(default, attempt.OccurredAt);
    }

    // --- Validaciones ---

    [Fact]
    public void Create_GivenEmptyChallengeId_ShouldThrowDomainException()
    {
        var challenge = CreateChallenge();

        Assert.Throws<DomainException>(() =>
            Attempt.Create(Guid.Empty, "selects all columns", elapsedSecs: 30, challenge));
    }

    [Fact]
    public void Create_GivenEmptyUserAnswer_ShouldThrowDomainException()
    {
        var challenge = CreateChallenge();

        Assert.Throws<DomainException>(() =>
            Attempt.Create(challenge.Id, "", elapsedSecs: 30, challenge));
    }

    [Fact]
    public void Create_GivenElapsedSecsZero_ShouldThrowDomainException()
    {
        var challenge = CreateChallenge();

        Assert.Throws<DomainException>(() =>
            Attempt.Create(challenge.Id, "selects all columns", elapsedSecs: 0, challenge));
    }

    [Fact]
    public void Create_GivenElapsedSecsExceedsTimeLimit_ShouldThrowDomainException()
    {
        var challenge = CreateChallenge(); // timeLimitSecs = 60

        Assert.Throws<DomainException>(() =>
            Attempt.Create(challenge.Id, "selects all columns", elapsedSecs: 61, challenge));
    }
}
