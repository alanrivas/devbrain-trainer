using DevBrain.Domain.Entities;
using DevBrain.Domain.Enums;
using DevBrain.Domain.Exceptions;

namespace DevBrain.Domain.Tests;

public class ChallengeTests
{
    // --- Helpers ---

    private static Challenge CreateValid() => Challenge.Create(
        title: "What does SELECT * do?",
        description: "Explain what SELECT * does in SQL.",
        category: ChallengeCategory.Sql,
        difficulty: Difficulty.Easy,
        correctAnswer: "selects all columns",
        timeLimitSecs: 60
    );

    // --- Creación válida ---

    [Fact]
    public void Create_GivenValidArguments_ShouldReturnChallenge()
    {
        var challenge = CreateValid();

        Assert.NotNull(challenge);
        Assert.NotEqual(Guid.Empty, challenge.Id);
        Assert.NotEqual(default, challenge.CreatedAt);
    }

    // --- Validación de título ---

    [Fact]
    public void Create_GivenEmptyTitle_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Challenge.Create(
            title: "",
            description: "Valid description",
            category: ChallengeCategory.Sql,
            difficulty: Difficulty.Easy,
            correctAnswer: "answer",
            timeLimitSecs: 60
        ));
    }

    [Fact]
    public void Create_GivenTitleWithFourCharacters_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Challenge.Create(
            title: "abcd",
            description: "Valid description",
            category: ChallengeCategory.Sql,
            difficulty: Difficulty.Easy,
            correctAnswer: "answer",
            timeLimitSecs: 60
        ));
    }

    // --- Validación de descripción ---

    [Fact]
    public void Create_GivenEmptyDescription_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Challenge.Create(
            title: "Valid title here",
            description: "",
            category: ChallengeCategory.Sql,
            difficulty: Difficulty.Easy,
            correctAnswer: "answer",
            timeLimitSecs: 60
        ));
    }

    // --- Validación de respuesta correcta ---

    [Fact]
    public void Create_GivenEmptyCorrectAnswer_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Challenge.Create(
            title: "Valid title here",
            description: "Valid description",
            category: ChallengeCategory.Sql,
            difficulty: Difficulty.Easy,
            correctAnswer: "",
            timeLimitSecs: 60
        ));
    }

    // --- Validación de tiempo límite ---

    [Fact]
    public void Create_GivenTimeLimitBelow30_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Challenge.Create(
            title: "Valid title here",
            description: "Valid description",
            category: ChallengeCategory.Sql,
            difficulty: Difficulty.Easy,
            correctAnswer: "answer",
            timeLimitSecs: 20
        ));
    }

    [Fact]
    public void Create_GivenTimeLimitAbove300_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Challenge.Create(
            title: "Valid title here",
            description: "Valid description",
            category: ChallengeCategory.Sql,
            difficulty: Difficulty.Easy,
            correctAnswer: "answer",
            timeLimitSecs: 400
        ));
    }

    // --- IsCorrectAnswer ---

    [Fact]
    public void IsCorrectAnswer_GivenExactMatch_ShouldReturnTrue()
    {
        var challenge = CreateValid();

        Assert.True(challenge.IsCorrectAnswer("selects all columns"));
    }

    [Fact]
    public void IsCorrectAnswer_GivenDifferentCasing_ShouldReturnTrue()
    {
        var challenge = CreateValid();

        Assert.True(challenge.IsCorrectAnswer("SELECTS ALL COLUMNS"));
    }

    [Fact]
    public void IsCorrectAnswer_GivenWrongAnswer_ShouldReturnFalse()
    {
        var challenge = CreateValid();

        Assert.False(challenge.IsCorrectAnswer("deletes all rows"));
    }
}
