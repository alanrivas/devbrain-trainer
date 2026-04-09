using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevBrain.Domain.Entities;
using DevBrain.Domain.Enums;
using DevBrain.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DevBrain.Infrastructure.Tests;

public class EFAttemptRepositoryTests
{
    private static DevBrainDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DevBrainDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new DevBrainDbContext(options);
    }

    private static EFAttemptRepository CreateRepository(DevBrainDbContext context)
    {
        return new EFAttemptRepository(context);
    }

    private static async Task<(User user, Challenge challenge)> SetupUserAndChallengeAsync(DevBrainDbContext context)
    {
        var testUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var user = User.Create(testUserId.ToString(), "test@example.com", "TestUser");
        var challenge = (await context.Challenges.FirstOrDefaultAsync())!;

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return (user, challenge);
    }

    [Fact]
    public async Task AddAsync_GivenValidAttempt_ShouldPersist()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var (user, challenge) = await SetupUserAndChallengeAsync(context);
        var attempt = Attempt.Create(challenge.Id, user.Id, "SELECT 1", 30, challenge);

        await repository.AddAsync(attempt);

        var retrieved = await context.Attempts.FirstOrDefaultAsync(a => a.Id == attempt.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(user.Id, retrieved.UserId);
    }

    [Fact]
    public async Task AddAsync_GivenMultipleAttemptsSameUser_ShouldPersistAll()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var (user, challenge) = await SetupUserAndChallengeAsync(context);

        var attempt1 = Attempt.Create(challenge.Id, user.Id, "answer1", 30, challenge);
        var attempt2 = Attempt.Create(challenge.Id, user.Id, "answer2", 45, challenge);

        await repository.AddAsync(attempt1);
        await repository.AddAsync(attempt2);

        var attempts = await context.Attempts
            .Where(a => a.UserId == user.Id)
            .ToListAsync();
        Assert.Equal(2, attempts.Count);
    }

    [Fact]
    public async Task AddAsync_GivenAttemptWithIsCorrectTrue_ShouldPersistCorrectly()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var (user, challenge) = await SetupUserAndChallengeAsync(context);
        var attempt = Attempt.Create(challenge.Id, user.Id, challenge.CorrectAnswer, 20, challenge);

        await repository.AddAsync(attempt);

        var retrieved = await context.Attempts.FirstOrDefaultAsync(a => a.Id == attempt.Id);
        Assert.NotNull(retrieved);
        Assert.True(retrieved.IsCorrect);
    }

    [Fact]
    public async Task AddAsync_GivenAttemptWithIsCorrectFalse_ShouldPersistCorrectly()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var (user, challenge) = await SetupUserAndChallengeAsync(context);
        var attempt = Attempt.Create(challenge.Id, user.Id, "wrong_answer", 50, challenge);

        await repository.AddAsync(attempt);

        var retrieved = await context.Attempts.FirstOrDefaultAsync(a => a.Id == attempt.Id);
        Assert.NotNull(retrieved);
        Assert.False(retrieved.IsCorrect);
    }

    [Fact]
    public async Task GetByUserAsync_GivenUserWithAttempts_ShouldReturnAll()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var (user, challenge) = await SetupUserAndChallengeAsync(context);

        var attempt1 = Attempt.Create(challenge.Id, user.Id, "answer1", 30, challenge);
        var attempt2 = Attempt.Create(challenge.Id, user.Id, "answer2", 45, challenge);

        await repository.AddAsync(attempt1);
        await repository.AddAsync(attempt2);

        var result = await repository.GetByUserAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByUserAsync_GivenUserWithoutAttempts_ShouldReturnEmpty()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        await SetupUserAndChallengeAsync(context);

        var result = await repository.GetByUserAsync(Guid.Empty);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByUserAsync_GivenEmptyUserId_ShouldReturnEmpty()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var result = await repository.GetByUserAsync(Guid.Empty);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByUserAsync_ShouldReturnOrderedByOccurredAtDescending()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var (user, challenge) = await SetupUserAndChallengeAsync(context);

        var attempt1 = Attempt.Create(challenge.Id, user.Id, "answer1", 30, challenge);
        await Task.Delay(10); // Ensure different timestamps
        var attempt2 = Attempt.Create(challenge.Id, user.Id, "answer2", 45, challenge);

        await repository.AddAsync(attempt1);
        await repository.AddAsync(attempt2);

        var result = await repository.GetByUserAsync(user.Id);

        // Last added should be first (DESC order)
        Assert.NotNull(result);
        Assert.True(result.Count >= 2);
        Assert.True(result[0].OccurredAt >= result[1].OccurredAt);
    }

    [Fact]
    public async Task GetLastByUserAsync_GivenUserWithAttempts_ShouldReturnMostRecent()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var (user, challenge) = await SetupUserAndChallengeAsync(context);

        var attempt1 = Attempt.Create(challenge.Id, user.Id, "answer1", 30, challenge);
        await Task.Delay(10);
        var attempt2 = Attempt.Create(challenge.Id, user.Id, "answer2", 45, challenge);

        await repository.AddAsync(attempt1);
        await repository.AddAsync(attempt2);

        var result = await repository.GetLastByUserAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal(attempt2.Id, result.Id);
    }

    [Fact]
    public async Task GetLastByUserAsync_GivenUserWithoutAttempts_ShouldReturnNull()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        await SetupUserAndChallengeAsync(context);

        var result = await repository.GetLastByUserAsync(Guid.Empty);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLastByUserAsync_GivenEmptyUserId_ShouldReturnNull()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var result = await repository.GetLastByUserAsync(Guid.Empty);

        Assert.Null(result);
    }

    [Fact]
    public async Task CountCorrectByUserAsync_GivenUserWithCorrectAndIncorrectAttempts_ShouldCountOnlyCorrect()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var (user, challenge) = await SetupUserAndChallengeAsync(context);

        var correctAttempt = Attempt.Create(challenge.Id, user.Id, challenge.CorrectAnswer, 20, challenge);
        var incorrectAttempt = Attempt.Create(challenge.Id, user.Id, "wrong", 50, challenge);
        var anotherCorrect = Attempt.Create(challenge.Id, user.Id, challenge.CorrectAnswer, 25, challenge);

        await repository.AddAsync(correctAttempt);
        await repository.AddAsync(incorrectAttempt);
        await repository.AddAsync(anotherCorrect);

        var result = await repository.CountCorrectByUserAsync(user.Id);

        Assert.Equal(2, result);
    }

    [Fact]
    public async Task CountCorrectByUserAsync_GivenUserWithoutCorrectAttempts_ShouldReturnZero()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var (user, challenge) = await SetupUserAndChallengeAsync(context);

        var incorrectAttempt = Attempt.Create(challenge.Id, user.Id, "wrong1", 30, challenge);
        var anotherIncorrect = Attempt.Create(challenge.Id, user.Id, "wrong2", 45, challenge);

        await repository.AddAsync(incorrectAttempt);
        await repository.AddAsync(anotherIncorrect);

        var result = await repository.CountCorrectByUserAsync(user.Id);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task CountCorrectByUserAsync_GivenUserWithoutAttempts_ShouldReturnZero()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        await SetupUserAndChallengeAsync(context);

        var result = await repository.CountCorrectByUserAsync(Guid.Empty);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task CountCorrectByUserAsync_GivenEmptyUserId_ShouldReturnZero()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var result = await repository.CountCorrectByUserAsync(Guid.Empty);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetByUserAsync_ShouldReturnReadOnlyList()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var (user, challenge) = await SetupUserAndChallengeAsync(context);
        var attempt = Attempt.Create(challenge.Id, user.Id, "answer", 30, challenge);

        await repository.AddAsync(attempt);

        var result = await repository.GetByUserAsync(user.Id);

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IReadOnlyList<Attempt>>(result);
    }

    [Fact]
    public async Task AddAsync_ThenGetByUserAsync_ShouldIncludeAddedAttempt()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var (user, challenge) = await SetupUserAndChallengeAsync(context);
        var attempt = Attempt.Create(challenge.Id, user.Id, "answer", 30, challenge);

        await repository.AddAsync(attempt);
        var result = await repository.GetByUserAsync(user.Id);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(attempt.Id, result[0].Id);
    }
}
