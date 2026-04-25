using DevBrain.Domain.Entities;
using DevBrain.Domain.Enums;
using DevBrain.Domain.Exceptions;
using DevBrain.Domain.ValueObjects;

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

    // --- CodeTestCase value object ---

    [Fact]
    public void CodeTestCase_Create_GivenValidArguments_ShouldCreate()
    {
        var tc = CodeTestCase.Create("2, 3", "5", "Returns sum of 2 and 3");

        Assert.Equal("2, 3", tc.Input);
        Assert.Equal("5", tc.ExpectedOutput);
        Assert.Equal("Returns sum of 2 and 3", tc.Description);
    }

    [Fact]
    public void CodeTestCase_Create_GivenEmptyExpectedOutput_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => CodeTestCase.Create("input", "", "description"));
    }

    [Fact]
    public void CodeTestCase_Create_GivenEmptyDescription_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => CodeTestCase.Create("input", "expected", ""));
    }

    // --- CreateCodeRunner ---

    [Fact]
    public void CreateCodeRunner_GivenThreeTestCases_ShouldHaveTypeCodeRunnerAndCorrectAnswerPass()
    {
        var testCases = new[]
        {
            CodeTestCase.Create("2, 3", "5", "Returns sum"),
            CodeTestCase.Create("0, 0", "0", "Zero sum"),
            CodeTestCase.Create("-1, 1", "0", "Negative sum")
        };

        var challenge = Challenge.CreateCodeRunner(
            title: "JS: Sum Two Numbers",
            description: "Write a function that sums two numbers.",
            category: ChallengeCategory.CodeLogic,
            difficulty: Difficulty.Easy,
            timeLimitSecs: 120,
            starterCode: "function solution(a, b) {}",
            testCases: testCases
        );

        Assert.Equal(ChallengeType.CodeRunner, challenge.Type);
        Assert.Equal("PASS", challenge.CorrectAnswer);
        Assert.Equal(3, challenge.TestCases.Count);
        Assert.Empty(challenge.Options);
    }

    [Fact]
    public void CreateCodeRunner_GivenOneTestCase_ShouldSucceed()
    {
        var challenge = Challenge.CreateCodeRunner(
            title: "JS: Sum Two Numbers",
            description: "Write a function that sums two numbers.",
            category: ChallengeCategory.CodeLogic,
            difficulty: Difficulty.Easy,
            timeLimitSecs: 120,
            starterCode: "",
            testCases: [CodeTestCase.Create("1", "1", "Identity")]
        );

        Assert.Equal(1, challenge.TestCases.Count);
    }

    [Fact]
    public void CreateCodeRunner_GivenSixTestCases_ShouldThrowDomainException()
    {
        var sixCases = Enumerable.Range(1, 6)
            .Select(i => CodeTestCase.Create(i.ToString(), i.ToString(), $"Case {i}"));

        Assert.Throws<DomainException>(() => Challenge.CreateCodeRunner(
            title: "JS: Sum Two Numbers",
            description: "Write a function that sums two numbers.",
            category: ChallengeCategory.CodeLogic,
            difficulty: Difficulty.Easy,
            timeLimitSecs: 120,
            starterCode: "",
            testCases: sixCases
        ));
    }

    [Fact]
    public void CreateCodeRunner_GivenZeroTestCases_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Challenge.CreateCodeRunner(
            title: "JS: Sum Two Numbers",
            description: "Write a function that sums two numbers.",
            category: ChallengeCategory.CodeLogic,
            difficulty: Difficulty.Easy,
            timeLimitSecs: 120,
            starterCode: "",
            testCases: Array.Empty<CodeTestCase>()
        ));
    }

    [Fact]
    public void IsCorrectAnswer_GivenPassInCodeRunner_ShouldReturnTrue()
    {
        var challenge = Challenge.CreateCodeRunner(
            title: "JS: Sum Two Numbers",
            description: "Write a function that sums two numbers.",
            category: ChallengeCategory.CodeLogic,
            difficulty: Difficulty.Easy,
            timeLimitSecs: 120,
            starterCode: "",
            testCases: [CodeTestCase.Create("1", "1", "Identity")]
        );

        Assert.True(challenge.IsCorrectAnswer("PASS"));
    }

    [Fact]
    public void IsCorrectAnswer_GivenFailInCodeRunner_ShouldReturnFalse()
    {
        var challenge = Challenge.CreateCodeRunner(
            title: "JS: Sum Two Numbers",
            description: "Write a function that sums two numbers.",
            category: ChallengeCategory.CodeLogic,
            difficulty: Difficulty.Easy,
            timeLimitSecs: 120,
            starterCode: "",
            testCases: [CodeTestCase.Create("1", "1", "Identity")]
        );

        Assert.False(challenge.IsCorrectAnswer("FAIL"));
    }

    // --- CreateOrdering ---

    [Fact]
    public void CreateOrdering_GivenFourItemsValidOrder_ShouldHaveTypeOrderingAndItems()
    {
        var items = new[] { "UI", "Infrastructure", "Domain", "Application" };
        var correctOrder = new[] { "Domain", "Application", "Infrastructure", "UI" };

        var challenge = Challenge.CreateOrdering(
            title: "Architecture: Hexagonal Layers",
            description: "Order from innermost to outermost.",
            category: ChallengeCategory.Architecture,
            difficulty: Difficulty.Medium,
            timeLimitSecs: 90,
            items: items,
            correctOrder: correctOrder
        );

        Assert.Equal(ChallengeType.Ordering, challenge.Type);
        Assert.Equal(4, challenge.Items.Count);
        Assert.Equal("Domain|Application|Infrastructure|UI", challenge.CorrectAnswer);
        Assert.Empty(challenge.Options);
    }

    [Fact]
    public void CreateOrdering_GivenTwoItems_ShouldSucceed()
    {
        var challenge = Challenge.CreateOrdering(
            title: "Order these two steps",
            description: "Which comes first?",
            category: ChallengeCategory.DevOps,
            difficulty: Difficulty.Easy,
            timeLimitSecs: 60,
            items: ["Build", "Deploy"],
            correctOrder: ["Build", "Deploy"]
        );

        Assert.Equal(2, challenge.Items.Count);
    }

    [Fact]
    public void CreateOrdering_GivenOneItem_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Challenge.CreateOrdering(
            title: "Single item ordering?",
            description: "This should fail.",
            category: ChallengeCategory.DevOps,
            difficulty: Difficulty.Easy,
            timeLimitSecs: 60,
            items: ["OnlyOne"],
            correctOrder: ["OnlyOne"]
        ));
    }

    [Fact]
    public void CreateOrdering_GivenSevenItems_ShouldThrowDomainException()
    {
        var items = new[] { "A", "B", "C", "D", "E", "F", "G" };

        Assert.Throws<DomainException>(() => Challenge.CreateOrdering(
            title: "Too many items challenge?",
            description: "This should fail.",
            category: ChallengeCategory.DevOps,
            difficulty: Difficulty.Easy,
            timeLimitSecs: 60,
            items: items,
            correctOrder: items
        ));
    }

    [Fact]
    public void CreateOrdering_GivenEmptyItem_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Challenge.CreateOrdering(
            title: "Empty item challenge?",
            description: "This should fail.",
            category: ChallengeCategory.DevOps,
            difficulty: Difficulty.Easy,
            timeLimitSecs: 60,
            items: ["A", "B", ""],
            correctOrder: ["A", "B", ""]
        ));
    }

    [Fact]
    public void CreateOrdering_GivenDuplicateItems_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Challenge.CreateOrdering(
            title: "Duplicate items challenge?",
            description: "This should fail.",
            category: ChallengeCategory.DevOps,
            difficulty: Difficulty.Easy,
            timeLimitSecs: 60,
            items: ["A", "B", "a"],
            correctOrder: ["A", "B", "a"]
        ));
    }

    [Fact]
    public void CreateOrdering_GivenCorrectOrderWithItemNotInItems_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Challenge.CreateOrdering(
            title: "Order not subset challenge?",
            description: "This should fail.",
            category: ChallengeCategory.DevOps,
            difficulty: Difficulty.Easy,
            timeLimitSecs: 60,
            items: ["A", "B", "C"],
            correctOrder: ["A", "B", "X"]
        ));
    }

    [Fact]
    public void CreateOrdering_GivenCorrectOrderWithFewerItemsThanItems_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Challenge.CreateOrdering(
            title: "Incomplete order challenge?",
            description: "This should fail.",
            category: ChallengeCategory.DevOps,
            difficulty: Difficulty.Easy,
            timeLimitSecs: 60,
            items: ["A", "B", "C"],
            correctOrder: ["A", "B"]
        ));
    }

    [Fact]
    public void IsCorrectAnswer_GivenCorrectPipeSeparatedOrder_ShouldReturnTrue()
    {
        var challenge = Challenge.CreateOrdering(
            title: "Architecture: Hexagonal Layers",
            description: "Order the layers.",
            category: ChallengeCategory.Architecture,
            difficulty: Difficulty.Medium,
            timeLimitSecs: 90,
            items: ["UI", "Infrastructure", "Domain", "Application"],
            correctOrder: ["Domain", "Application", "Infrastructure", "UI"]
        );

        Assert.True(challenge.IsCorrectAnswer("Domain|Application|Infrastructure|UI"));
    }

    [Fact]
    public void IsCorrectAnswer_GivenWrongOrderInOrdering_ShouldReturnFalse()
    {
        var challenge = Challenge.CreateOrdering(
            title: "Architecture: Hexagonal Layers",
            description: "Order the layers.",
            category: ChallengeCategory.Architecture,
            difficulty: Difficulty.Medium,
            timeLimitSecs: 90,
            items: ["UI", "Infrastructure", "Domain", "Application"],
            correctOrder: ["Domain", "Application", "Infrastructure", "UI"]
        );

        Assert.False(challenge.IsCorrectAnswer("UI|Infrastructure|Domain|Application"));
    }
}
