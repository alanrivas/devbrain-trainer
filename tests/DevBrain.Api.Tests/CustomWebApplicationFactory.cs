using DevBrain.Domain.Enums;
using DevBrain.Domain.Interfaces;
using DevBrain.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DevBrain.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Add in-memory database for testing
            services.AddDbContext<DevBrainDbContext>(options =>
                options.UseInMemoryDatabase("DevBrainTestDb")
            );
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
        db.Database.EnsureCreated();

        // If challenges already exist, seed is complete
        if (await db.Challenges.AnyAsync())
            return;

        // Seed 10 challenges with known answers for testing
        var challenges = new[]
        {
            Domain.Entities.Challenge.Create("Test Challenge 1", "Correct answer is YES", ChallengeCategory.Sql, Difficulty.Easy, "YES", 30),
            Domain.Entities.Challenge.Create("Test Challenge 2", "Correct answer is CORRECT", ChallengeCategory.CodeLogic, Difficulty.Easy, "CORRECT", 60),
            Domain.Entities.Challenge.Create("System Design", "Design a scalable architecture", ChallengeCategory.Architecture, Difficulty.Hard, "microservices", 200),
            Domain.Entities.Challenge.Create("Docker Deployment", "Deploy a container", ChallengeCategory.DevOps, Difficulty.Medium, "docker run", 120),
            Domain.Entities.Challenge.Create("SQL Query", "Write a query", ChallengeCategory.Sql, Difficulty.Medium, "SELECT * FROM users", 120),
            Domain.Entities.Challenge.Create("Array Sorting", "Sort array", ChallengeCategory.CodeLogic, Difficulty.Medium, "ascending", 120),
            Domain.Entities.Challenge.Create("Database Indexing", "Optimize with indices", ChallengeCategory.Architecture, Difficulty.Hard, "B-tree", 150),
            Domain.Entities.Challenge.Create("Kubernetes Basics", "What is a Pod?", ChallengeCategory.DevOps, Difficulty.Easy, "smallest deployable unit", 90),
            Domain.Entities.Challenge.Create("Complex SQL", "Window functions", ChallengeCategory.Sql, Difficulty.Hard, "ROW_NUMBER() OVER (ORDER BY)", 180),
            Domain.Entities.Challenge.Create("Memory Test", "Recall number", ChallengeCategory.WorkingMemory, Difficulty.Medium, "12345", 60),
        };

        foreach (var challenge in challenges)
        {
            await repo.AddAsync(challenge);
        }
    }
}
