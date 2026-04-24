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

    // --- ChallengeType: OpenText defaults ---

    [Fact]
    public void Create_GivenValidArguments_ShouldHaveTypeOpenText()
    {
        var challenge = CreateValid();

        Assert.Equal(ChallengeType.OpenText, challenge.Type);
        Assert.Empty(challenge.Options);
    }

    // --- CreateMultipleChoice: casos válidos ---

    [Fact]
    public void CreateMultipleChoice_GivenFourValidOptions_ShouldHaveTypeMultipleChoice()
    {
        var challenge = Challenge.CreateMultipleChoice(
            title: "What is binary search complexity?",
            description: "Select the correct time complexity.",
            category: ChallengeCategory.CodeLogic,
            difficulty: Difficulty.Medium,
            correctAnswer: "O(log n)",
            timeLimitSecs: 60,
            options: ["O(n)", "O(n²)", "O(log n)", "O(n log n)"]
        );

        Assert.Equal(ChallengeType.MultipleChoice, challenge.Type);
        Assert.Equal(4, challenge.Options.Count);
        Assert.NotEqual(Guid.Empty, challenge.Id);
    }

    [Fact]
    public void CreateMultipleChoice_GivenTwoOptions_ShouldSucceed()
    {
        var challenge = Challenge.CreateMultipleChoice(
            title: "Is INNER JOIN the default JOIN?",
            description: "True or false.",
            category: ChallengeCategory.Sql,
            difficulty: Difficulty.Easy,
            correctAnswer: "True",
            timeLimitSecs: 45,
            options: ["True", "False"]
        );

        Assert.Equal(2, challenge.Options.Count);
    }

    // --- CreateMultipleChoice: validaciones de opciones ---

    [Fact]
    public void CreateMultipleChoice_GivenOneOption_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Challenge.CreateMultipleChoice(
            title: "Only one option challenge?",
            description: "This should fail.",
            category: ChallengeCategory.Sql,
            difficulty: Difficulty.Easy,
            correctAnswer: "A",
            timeLimitSecs: 60,
            options: ["A"]
        ));
    }

    [Fact]
    public void CreateMultipleChoice_GivenFiveOptions_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Challenge.CreateMultipleChoice(
            title: "Too many options challenge?",
            description: "This should fail.",
            category: ChallengeCategory.Sql,
            difficulty: Difficulty.Easy,
            correctAnswer: "A",
            timeLimitSecs: 60,
            options: ["A", "B", "C", "D", "E"]
        ));
    }

    [Fact]
    public void CreateMultipleChoice_GivenEmptyOption_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Challenge.CreateMultipleChoice(
            title: "Empty option challenge?",
            description: "This should fail.",
            category: ChallengeCategory.Sql,
            difficulty: Difficulty.Easy,
            correctAnswer: "A",
            timeLimitSecs: 60,
            options: ["A", "B", ""]
        ));
    }

    [Fact]
    public void CreateMultipleChoice_GivenWhitespaceOption_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Challenge.CreateMultipleChoice(
            title: "Whitespace option challenge?",
            description: "This should fail.",
            category: ChallengeCategory.Sql,
            difficulty: Difficulty.Easy,
            correctAnswer: "A",
            timeLimitSecs: 60,
            options: ["A", "B", "   "]
        ));
    }

    [Fact]
    public void CreateMultipleChoice_GivenDuplicateOptions_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Challenge.CreateMultipleChoice(
            title: "Duplicate options challenge?",
            description: "This should fail.",
            category: ChallengeCategory.Sql,
            difficulty: Difficulty.Easy,
            correctAnswer: "A",
            timeLimitSecs: 60,
            options: ["A", "B", "a"]
        ));
    }

    [Fact]
    public void CreateMultipleChoice_GivenCorrectAnswerNotInOptions_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Challenge.CreateMultipleChoice(
            title: "Answer not in options?",
            description: "This should fail.",
            category: ChallengeCategory.Sql,
            difficulty: Difficulty.Easy,
            correctAnswer: "X",
            timeLimitSecs: 60,
            options: ["A", "B", "C"]
        ));
    }

    // --- IsCorrectAnswer en MultipleChoice ---

    [Fact]
    public void IsCorrectAnswer_GivenCorrectOptionInMultipleChoice_ShouldReturnTrue()
    {
        var challenge = Challenge.CreateMultipleChoice(
            title: "What is binary search complexity?",
            description: "Select the correct time complexity.",
            category: ChallengeCategory.CodeLogic,
            difficulty: Difficulty.Medium,
            correctAnswer: "O(log n)",
            timeLimitSecs: 60,
            options: ["O(n)", "O(n²)", "O(log n)", "O(n log n)"]
        );

        Assert.True(challenge.IsCorrectAnswer("O(log n)"));
    }

    [Fact]
    public void IsCorrectAnswer_GivenWrongOptionInMultipleChoice_ShouldReturnFalse()
    {
        var challenge = Challenge.CreateMultipleChoice(
            title: "What is binary search complexity?",
            description: "Select the correct time complexity.",
            category: ChallengeCategory.CodeLogic,
            difficulty: Difficulty.Medium,
            correctAnswer: "O(log n)",
            timeLimitSecs: 60,
            options: ["O(n)", "O(n²)", "O(log n)", "O(n log n)"]
        );

        Assert.False(challenge.IsCorrectAnswer("O(n)"));
    }
}
