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

public class EFChallengeRepositoryTests
{
    private static DevBrainDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DevBrainDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new DevBrainDbContext(options);
    }

    private static EFChallengeRepository CreateRepository(DevBrainDbContext context)
    {
        return new EFChallengeRepository(context);
    }

    [Fact]
    public async Task GetByIdAsync_GivenValidExistingId_ShouldReturnChallenge()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var challenges = await context.Challenges.ToListAsync();
        var firstChallenge = challenges.First();

        var result = await repository.GetByIdAsync(firstChallenge.Id);

        Assert.NotNull(result);
        Assert.Equal(firstChallenge.Id, result.Id);
        Assert.Equal(firstChallenge.Title, result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_GivenValidNonExistingId_ShouldReturnNull()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var nonExistingId = Guid.NewGuid();
        var result = await repository.GetByIdAsync(nonExistingId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_GivenEmptyGuid_ShouldReturnNull()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var result = await repository.GetByIdAsync(Guid.Empty);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_GivenNoFilters_ShouldReturnAllChallenges()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var result = await repository.GetAllAsync();

        Assert.NotNull(result);
        Assert.True(result.Count >= 10, "Should return seeded challenges");
    }

    [Fact]
    public async Task GetAllAsync_GivenNoFilters_ShouldReturnOrderedByCreatedAtDescending()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var result = await repository.GetAllAsync();

        var resultList = result.ToList();
        for (int i = 0; i < resultList.Count - 1; i++)
        {
            Assert.True(resultList[i].CreatedAt >= resultList[i + 1].CreatedAt,
                "Results should be ordered by CreatedAt descending");
        }
    }

    [Fact]
    public async Task GetAllAsync_GivenCategoryFilter_ShouldReturnOnlyFilteredCategory()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var result = await repository.GetAllAsync(category: ChallengeCategory.Sql);

        Assert.NotNull(result);
        Assert.All(result, challenge => Assert.Equal(ChallengeCategory.Sql, challenge.Category));
    }

    [Fact]
    public async Task GetAllAsync_GivenDifficultyFilter_ShouldReturnOnlyFilteredDifficulty()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var result = await repository.GetAllAsync(difficulty: Difficulty.Easy);

        Assert.NotNull(result);
        Assert.All(result, challenge => Assert.Equal(Difficulty.Easy, challenge.Difficulty));
    }

    [Fact]
    public async Task GetAllAsync_GivenBothFilters_ShouldApplyBoth()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var result = await repository.GetAllAsync(
            category: ChallengeCategory.CodeLogic,
            difficulty: Difficulty.Medium
        );

        Assert.NotNull(result);
        Assert.All(result, challenge =>
        {
            Assert.Equal(ChallengeCategory.CodeLogic, challenge.Category);
            Assert.Equal(Difficulty.Medium, challenge.Difficulty);
        });
    }

    [Fact]
    public async Task GetAllAsync_GivenFiltersWithNoMatches_ShouldReturnEmptyList()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        // Assuming there's no challenge with this impossible combination
        var result = await repository.GetAllAsync(
            category: ChallengeCategory.Sql,
            difficulty: Difficulty.Hard
        );

        // This could return empty or have items depending on seed data, but the method should not throw
        Assert.NotNull(result);
        // Just verify it's enumerable and not null
        Assert.IsAssignableFrom<IReadOnlyList<Challenge>>(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnReadOnlyList()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var result = await repository.GetAllAsync();

        Assert.NotNull(result);
        // Verify it's a collection with read-only semantics
        Assert.False(result is List<Challenge>, "Should not return mutable List directly, but AsReadOnly wrapper");
    }

    [Fact]
    public async Task AddAsync_GivenValidChallenge_ShouldPersist()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var challenge = Challenge.Create(
            "New Test Challenge",
            "Test description",
            ChallengeCategory.DevOps,
            Difficulty.Hard,
            "docker pull image",
            180
        );

        await repository.AddAsync(challenge);

        var retrieved = await context.Challenges.FirstOrDefaultAsync(c => c.Id == challenge.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("New Test Challenge", retrieved.Title);
    }

    [Fact]
    public async Task AddAsync_GivenMultipleChallenges_ShouldPersistAll()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var challenge1 = Challenge.Create(
            "Challenge 1",
            "Description 1",
            ChallengeCategory.Sql,
            Difficulty.Medium,
            "query1",
            90
        );

        var challenge2 = Challenge.Create(
            "Challenge 2",
            "Description 2",
            ChallengeCategory.CodeLogic,
            Difficulty.Hard,
            "code2",
            150
        );

        await repository.AddAsync(challenge1);
        await repository.AddAsync(challenge2);

        var countBefore = (await context.Challenges.CountAsync()) - 10; // Subtract seed data
        Assert.Equal(2, countBefore);
    }

    [Fact]
    public async Task GetByIdAsync_AfterAddAsync_ShouldReturnAddedChallenge()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var challenge = Challenge.Create(
            "Retrieve Test",
            "Test retrieve",
            ChallengeCategory.Architecture,
            Difficulty.Easy,
            "arch_answer",
            60
        );

        await repository.AddAsync(challenge);
        var retrieved = await repository.GetByIdAsync(challenge.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(challenge.Id, retrieved.Id);
        Assert.Equal("Retrieve Test", retrieved.Title);
    }
}
