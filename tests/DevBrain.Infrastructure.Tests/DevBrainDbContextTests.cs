using System;
using System.Linq;
using System.Threading.Tasks;
using DevBrain.Domain.Entities;
using DevBrain.Domain.Enums;
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

        var userId = "123e4567-e89b-12d3-a456-426614174000";
        var user = User.Create(userId, "user@example.com", "TestUser");
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

        var userId = "123e4567-e89b-12d3-a456-426614174000";
        var user = User.Create(userId, "user@example.com", "TestUser");
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

        var userId = "123e4567-e89b-12d3-a456-426614174000";
        var user = User.Create(userId, "user@example.com", "TestUser");
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
}
