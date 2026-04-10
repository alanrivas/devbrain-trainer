using System;
using System.Threading.Tasks;
using DevBrain.Domain.Entities;
using DevBrain.Domain.Enums;
using DevBrain.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DevBrain.Infrastructure.Tests;

public class EFBadgeRepositoryTests
{
    private static DevBrainDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DevBrainDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DevBrainDbContext(options);
    }

    private static EFBadgeRepository CreateRepository(DevBrainDbContext context)
        => new EFBadgeRepository(context);

    private static async Task<User> CreateUserAsync(DevBrainDbContext context, string email = "badge-user@example.com")
    {
        var user = User.CreateFromRegistration(email, "hashedpw123", "Badge User");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task AddAsync_GivenValidBadge_ShouldPersist()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var user = await CreateUserAsync(context);
        var badge = UserBadge.Create(user.Id, BadgeType.FirstBlood);

        await repository.AddAsync(badge);

        var retrieved = await context.UserBadges.FirstOrDefaultAsync(b => b.Id == badge.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(user.Id, retrieved.UserId);
        Assert.Equal(BadgeType.FirstBlood, retrieved.Type);
    }

    [Fact]
    public async Task GetByUserAsync_GivenUserWithNoBadges_ShouldReturnEmpty()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var user = await CreateUserAsync(context);

        var result = await repository.GetByUserAsync(user.Id);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByUserAsync_GivenTwoBadges_ShouldReturnBothOrderedByEarnedAtAsc()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var user = await CreateUserAsync(context);
        var badge1 = UserBadge.Create(user.Id, BadgeType.FirstBlood);
        await Task.Delay(10);
        var badge2 = UserBadge.Create(user.Id, BadgeType.Brave);

        await repository.AddAsync(badge1);
        await repository.AddAsync(badge2);

        var result = await repository.GetByUserAsync(user.Id);

        Assert.Equal(2, result.Count);
        Assert.True(result[0].EarnedAt <= result[1].EarnedAt);
    }

    [Fact]
    public async Task GetByUserAsync_GivenTwoUsers_ShouldReturnOnlyForRequestedUser()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var user1 = User.CreateFromRegistration("user1@example.com", "hash", "User1");
        var user2 = User.CreateFromRegistration("user2@example.com", "hash", "User2");
        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        var badge1 = UserBadge.Create(user1.Id, BadgeType.FirstBlood);
        var badge2 = UserBadge.Create(user2.Id, BadgeType.Brave);

        await repository.AddAsync(badge1);
        await repository.AddAsync(badge2);

        var result = await repository.GetByUserAsync(user1.Id);

        Assert.Single(result);
        Assert.Equal(user1.Id, result[0].UserId);
    }

    [Fact]
    public async Task HasBadgeAsync_GivenBadgeNotEarned_ShouldReturnFalse()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var user = await CreateUserAsync(context);

        var result = await repository.HasBadgeAsync(user.Id, BadgeType.FirstBlood);

        Assert.False(result);
    }

    [Fact]
    public async Task HasBadgeAsync_GivenBadgeEarned_ShouldReturnTrue()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        var repository = CreateRepository(context);

        var user = await CreateUserAsync(context);
        var badge = UserBadge.Create(user.Id, BadgeType.FirstBlood);
        await repository.AddAsync(badge);

        var result = await repository.HasBadgeAsync(user.Id, BadgeType.FirstBlood);

        Assert.True(result);
    }
}
