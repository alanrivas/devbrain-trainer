using System;
using System.Linq;
using System.Threading.Tasks;
using DevBrain.Domain.Entities;
using DevBrain.Domain.Enums;
using DevBrain.Domain.ValueObjects;
using DevBrain.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DevBrain.Infrastructure.Tests;

public class DevBrainDbContextTests
{
    private static DevBrainDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DevBrainDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new DevBrainDbContext(options);
    }

    [Fact]
    public void DbContext_GivenInMemoryDatabase_ShouldCreateContext()
    {
        using var context = CreateDbContext();
        Assert.NotNull(context);
    }

    [Fact]
    public void Database_GivenInMemoryDatabase_ShouldHaveDbSets()
    {
        using var context = CreateDbContext();
        
        Assert.NotNull(context.Users);
        Assert.NotNull(context.Challenges);
        Assert.NotNull(context.Attempts);
    }

    [Fact]
    public async Task Users_GivenValidUser_ShouldInsertSuccessfully()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();

        var user = User.Create("123e4567-e89b-12d3-a456-426614174000", "user@example.com", "TestUser");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var saved = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.NotNull(saved);
        Assert.Equal("user@example.com", saved.Email);
    }

    [Fact]
    public async Task Challenges_GivenValidChallenge_ShouldInsertSuccessfully()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();

        var challenge = Challenge.Create(
            "SQL Challenge",
            "Write a query to get all users",
            ChallengeCategory.Sql,
            Difficulty.Easy,
            "SELECT * FROM users",
            60
        );
        context.Challenges.Add(challenge);
        await context.SaveChangesAsync();

        var saved = await context.Challenges.FirstOrDefaultAsync(c => c.Id == challenge.Id);
        Assert.NotNull(saved);
        Assert.Equal("SQL Challenge", saved.Title);
    }

    [Fact]
    public async Task Attempts_GivenValidAttempt_ShouldInsertSuccessfully()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();

        var userId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000");
        var user = User.Create(userId.ToString(), "user@example.com", "TestUser");
        var challenge = Challenge.Create(
            "SQL Challenge",
            "Write a query",
            ChallengeCategory.Sql,
            Difficulty.Easy,
            "SELECT 1",
            60
        );

        context.Users.Add(user);
        context.Challenges.Add(challenge);
        await context.SaveChangesAsync();

        var attempt = Attempt.Create(challenge.Id, userId, "SELECT 1", 30, challenge);
        context.Attempts.Add(attempt);
        await context.SaveChangesAsync();

        var saved = await context.Attempts.FirstOrDefaultAsync(a => a.Id == attempt.Id);
        Assert.NotNull(saved);
        Assert.Equal(userId, saved.UserId);
        Assert.Equal(challenge.Id, saved.ChallengeId);
    }

    [Fact]
    public async Task Attempts_GivenRelationshipConfigured_ShouldNavigateToUser()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();

        var userId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000");
        var user = User.Create(userId.ToString(), "user@example.com", "TestUser");
        var challenge = Challenge.Create(
            "Code Logic Challenge",
            "Refactor this code",
            ChallengeCategory.CodeLogic,
            Difficulty.Medium,
            "refactored_code",
            120
        );

        context.Users.Add(user);
        context.Challenges.Add(challenge);
        await context.SaveChangesAsync();

        var attempt = Attempt.Create(challenge.Id, userId, "wrong_answer", 45, challenge);
        context.Attempts.Add(attempt);
        await context.SaveChangesAsync();

        var savedAttempt = await context.Attempts
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == attempt.Id);

        Assert.NotNull(savedAttempt);
        Assert.NotNull(savedAttempt.User);
        Assert.Equal("TestUser", savedAttempt.User.DisplayName);
    }

    [Fact]
    public async Task Attempts_GivenRelationshipConfigured_ShouldNavigateToChallenge()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();

        var userId = Guid.Parse("123e4567-e89b-12d3-a456-426614174001");
        var user = User.Create(userId.ToString(), "user@example.com", "TestUser");
        var challenge = Challenge.Create(
            "Architecture Challenge",
            "Design a system",
            ChallengeCategory.Architecture,
            Difficulty.Hard,
            "proper_design",
            300
        );

        context.Users.Add(user);
        context.Challenges.Add(challenge);
        await context.SaveChangesAsync();

        var attempt = Attempt.Create(challenge.Id, userId, "bad_design", 250, challenge);
        context.Attempts.Add(attempt);
        await context.SaveChangesAsync();

        var savedAttempt = await context.Attempts
            .Include(a => a.Challenge)
            .FirstOrDefaultAsync(a => a.Id == attempt.Id);

        Assert.NotNull(savedAttempt);
        Assert.NotNull(savedAttempt.Challenge);
        Assert.Equal("Architecture Challenge", savedAttempt.Challenge.Title);
    }

    [Fact]
    public async Task Challenges_GivenSeeded_ShouldHaveAtLeast10Challenges()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();

        var count = await context.Challenges.CountAsync();
        Assert.True(count >= 10, $"Expected at least 10 seeded challenges, but found {count}");
    }

    [Fact]
    public async Task Challenges_GivenSeeded_ShouldHaveDifferentCategoriesAndDifficulties()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();

        var challenges = await context.Challenges.ToListAsync();

        var categories = challenges.Select(c => c.Category).Distinct().Count();
        var difficulties = challenges.Select(c => c.Difficulty).Distinct().Count();

        Assert.True(categories > 1, "Expected challenges in multiple categories");
        Assert.True(difficulties > 1, "Expected challenges with different difficulties");
    }

    // --- ChallengeType + Options persistence ---

    [Fact]
    public async Task Challenge_GivenOpenTextChallenge_ShouldPersistTypeAndEmptyOptions()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();

        var challenge = Challenge.Create(
            "Valid title here",
            "A description for this challenge",
            ChallengeCategory.Sql,
            Difficulty.Easy,
            "SELECT 1",
            60
        );
        context.Challenges.Add(challenge);
        await context.SaveChangesAsync();

        var saved = await context.Challenges.FirstAsync(c => c.Id == challenge.Id);
        Assert.Equal(ChallengeType.OpenText, saved.Type);
        Assert.Empty(saved.Options);
    }

    [Fact]
    public async Task Challenge_GivenMultipleChoiceChallenge_ShouldPersistTypeAndOptions()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();

        var challenge = Challenge.CreateMultipleChoice(
            title: "What is the null-coalescing operator?",
            description: "Select the correct operator.",
            category: ChallengeCategory.CodeLogic,
            difficulty: Difficulty.Easy,
            correctAnswer: "??",
            timeLimitSecs: 45,
            options: ["?.", "??", "?:", "!."]
        );
        context.Challenges.Add(challenge);
        await context.SaveChangesAsync();

        var saved = await context.Challenges.FirstAsync(c => c.Id == challenge.Id);
        Assert.Equal(ChallengeType.MultipleChoice, saved.Type);
        Assert.Equal(4, saved.Options.Count);
        Assert.Contains("??", saved.Options);
    }

    [Fact]
    public async Task Challenges_GivenSeeded_ShouldIncludeMultipleChoiceChallenges()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();

        var mcChallenges = await context.Challenges
            .Where(c => c.Type == ChallengeType.MultipleChoice)
            .ToListAsync();

        Assert.True(mcChallenges.Count >= 3, $"Expected at least 3 seeded MultipleChoice challenges, got {mcChallenges.Count}");
    }

    [Fact]
    public async Task MultipleChoiceChallenge_GivenRoundTrip_ShouldPreserveOptionsOrder()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();

        var options = new[] { "LEFT JOIN", "RIGHT JOIN", "INNER JOIN", "FULL OUTER JOIN" };
        var challenge = Challenge.CreateMultipleChoice(
            title: "Which JOIN returns matching rows only?",
            description: "Select the correct JOIN type.",
            category: ChallengeCategory.Sql,
            difficulty: Difficulty.Easy,
            correctAnswer: "INNER JOIN",
            timeLimitSecs: 45,
            options: options
        );
        context.Challenges.Add(challenge);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var saved = await context.Challenges.FirstAsync(c => c.Id == challenge.Id);

        Assert.Equal(options.Length, saved.Options.Count);
        for (int i = 0; i < options.Length; i++)
            Assert.Equal(options[i], saved.Options[i]);
    }

    // --- CodeRunner persistence ---

    [Fact]
    public async Task CodeRunnerChallenge_GivenRoundTrip_ShouldPersistTestCases()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();

        var testCases = new[]
        {
            CodeTestCase.Create("2, 3", "5", "Returns sum of 2 and 3"),
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
        context.Challenges.Add(challenge);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var saved = await context.Challenges.FirstAsync(c => c.Id == challenge.Id);

        Assert.Equal(ChallengeType.CodeRunner, saved.Type);
        Assert.Equal("PASS", saved.CorrectAnswer);
        Assert.Equal(3, saved.TestCases.Count);
        Assert.Equal("5", saved.TestCases[0].ExpectedOutput);
        Assert.Equal("Returns sum of 2 and 3", saved.TestCases[0].Description);
    }

    [Fact]
    public async Task CodeRunnerChallenge_GivenRoundTrip_ShouldPersistStarterCode()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();

        var challenge = Challenge.CreateCodeRunner(
            title: "JS: Sum Two Numbers",
            description: "Write a function that sums two numbers.",
            category: ChallengeCategory.CodeLogic,
            difficulty: Difficulty.Easy,
            timeLimitSecs: 120,
            starterCode: "function solution(a, b) {\n  // your code here\n}",
            testCases: [CodeTestCase.Create("1", "1", "Identity")]
        );
        context.Challenges.Add(challenge);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var saved = await context.Challenges.FirstAsync(c => c.Id == challenge.Id);

        Assert.Equal("function solution(a, b) {\n  // your code here\n}", saved.StarterCode);
    }

    [Fact]
    public async Task OpenTextChallenge_GivenRoundTrip_ShouldHaveEmptyStarterCodeAndTestCases()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();

        var challenge = Challenge.Create(
            title: "What does SELECT * do?",
            description: "Explain SELECT *.",
            category: ChallengeCategory.Sql,
            difficulty: Difficulty.Easy,
            correctAnswer: "selects all columns",
            timeLimitSecs: 60
        );
        context.Challenges.Add(challenge);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var saved = await context.Challenges.FirstAsync(c => c.Id == challenge.Id);

        Assert.Equal(string.Empty, saved.StarterCode ?? string.Empty);
        Assert.True((saved.TestCases?.Count ?? 0) == 0, "OpenText challenge should have no test cases");
    }
}
