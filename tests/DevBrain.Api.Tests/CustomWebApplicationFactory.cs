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

        if (await db.Challenges.AnyAsync())
            return; // Already seeded

        // Seed 10 challenges across categories and difficulties
        var challenges = new[]
        {
            Domain.Entities.Challenge.Create("SQL SELECT Performance", "Optimize a SELECT query with multiple JOINs", ChallengeCategory.Sql, Difficulty.Medium, "SELECT * FROM orders JOIN customers", 300),
            Domain.Entities.Challenge.Create("Recursion Basics", "Write a recursive factorial function", ChallengeCategory.CodeLogic, Difficulty.Easy, "5", 180),
            Domain.Entities.Challenge.Create("System Design", "Design a scalable architecture", ChallengeCategory.Architecture, Difficulty.Hard, "microservices", 600),
            Domain.Entities.Challenge.Create("Docker Deployment", "Deploy a container", ChallengeCategory.DevOps, Difficulty.Medium, "docker run", 300),
            Domain.Entities.Challenge.Create("SQL Joins", "Identify the type of JOIN used", ChallengeCategory.Sql, Difficulty.Easy, "INNER JOIN", 120),
            Domain.Entities.Challenge.Create("Array Sorting", "Sort an array in ascending order", ChallengeCategory.CodeLogic, Difficulty.Easy, "true", 120),
            Domain.Entities.Challenge.Create("Database Indexing", "How to optimize queries with indices?", ChallengeCategory.Architecture, Difficulty.Hard, "B-tree", 600),
            Domain.Entities.Challenge.Create("Kubernetes Basics", "What is a Pod?", ChallengeCategory.DevOps, Difficulty.Easy, "smallest deployable unit", 60),
            Domain.Entities.Challenge.Create("Complex SQL Query", "Write a window function query", ChallengeCategory.Sql, Difficulty.Hard, "ROW_NUMBER() OVER (ORDER BY)", 900),
            Domain.Entities.Challenge.Create("Memory Retention", "Recall a 5-digit number after 10 seconds", ChallengeCategory.WorkingMemory, Difficulty.Medium, "12345", 10),
        };

        await repo.AddAsync(challenges[0]);
        await repo.AddAsync(challenges[1]);
        await repo.AddAsync(challenges[2]);
        await repo.AddAsync(challenges[3]);
        await repo.AddAsync(challenges[4]);
        await repo.AddAsync(challenges[5]);
        await repo.AddAsync(challenges[6]);
        await repo.AddAsync(challenges[7]);
        await repo.AddAsync(challenges[8]);
        await repo.AddAsync(challenges[9]);
    }
}
