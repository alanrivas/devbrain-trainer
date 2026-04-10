using DevBrain.Api.Services;
using DevBrain.Domain.Enums;
using DevBrain.Domain.Interfaces;
using DevBrain.Infrastructure.Persistence;
using DevBrain.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace DevBrain.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private DevBrainDbContext? _dbContext;
    private readonly string _dbName = $"DevBrainTestDb_{Guid.NewGuid()}";  // Unique per factory

    public CustomWebApplicationFactory()
    {
        // Signal to Program.cs that we're running in a test environment
        Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_TEST", "true");
    }

    public async Task<DevBrainDbContext> GetDbContextAsync()
    {
        if (_dbContext == null)
        {
            var scope = Services.CreateScope();
            _dbContext = scope.ServiceProvider.GetRequiredService<DevBrainDbContext>();
        }
        return _dbContext;
    }

    public record TestChallenge(string Title, string CorrectAnswer);

    public static List<TestChallenge> GetTestChallenges() => new()
    {
        new("Test Challenge 1", "YES"),
        new("Test Challenge 2", "CORRECT"),
        new("System Design", "microservices"),
        new("Docker Deployment", "docker run"),
        new("SQL Query", "SELECT * FROM users"),
        new("Array Sorting", "ascending"),
        new("Database Indexing", "B-tree"),
        new("Kubernetes Basics", "smallest deployable unit"),
        new("Complex SQL", "ROW_NUMBER() OVER (ORDER BY)"),
        new("Memory Test", "12345"),
    };
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Add in-memory database for testing with unique name to avoid cross-test pollution
            // Program.cs won't register Npgsql because we set DOTNET_RUNNING_IN_TEST
            services.AddDbContext<DevBrainDbContext>(options =>
                options.UseInMemoryDatabase(_dbName),
                ServiceLifetime.Scoped
            );

            // Ensure these services are registered (Program.cs registers them but we can re-add)
            services.AddScoped<IUserRepository, EFUserRepository>();
            services.AddScoped<IPasswordHashService, PasswordHashService>();
            services.AddScoped<IChallengeRepository, EFChallengeRepository>();
            services.AddScoped<IAttemptRepository, EFAttemptRepository>();

            // Redis + Streak (real Redis at localhost:6379 — needed for AttemptService + GetUserStats)
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect("localhost:6379")
            );
            services.AddScoped<IStreakService, RedisStreakService>();
        });

        // Seed data after app is built
        builder.ConfigureServices(services =>
        {
            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DevBrainDbContext>();
            var repo = scope.ServiceProvider.GetRequiredService<IChallengeRepository>();

            SeedTestData(db, repo).Wait();
        });
    }

    private async Task SeedTestData(DevBrainDbContext db, IChallengeRepository repo)
    {
        await db.Database.EnsureCreatedAsync();

        // Clear any existing challenges (from OnModelCreating seed data in production mode)
        // This ensures tests always use clean test seed data, not production data
        var allChallenges = await db.Challenges.ToListAsync();
        foreach (var challenge in allChallenges)
        {
            db.Challenges.Remove(challenge);
        }
        await db.SaveChangesAsync();

        // Seed 10 test challenges with known answers only
        var testChallenges = GetTestChallenges();
        var categories = new[] { ChallengeCategory.Sql, ChallengeCategory.CodeLogic, ChallengeCategory.Architecture, ChallengeCategory.DevOps, ChallengeCategory.WorkingMemory };
        var difficulties = new[] { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard };

        for (int i = 0; i < testChallenges.Count; i++)
        {
            var testChallenge = testChallenges[i];
            var timeLimit = Math.Min(60 + (i * 30), 300); // Cap at 300 max
            var challenge = Domain.Entities.Challenge.Create(
                testChallenge.Title,
                $"Test description for {testChallenge.Title}",
                categories[i % categories.Length],
                difficulties[i % difficulties.Length],
                testChallenge.CorrectAnswer,
                timeLimit
            );
            await repo.AddAsync(challenge);
        }
    }
}
