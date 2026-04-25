using System.Text.Json;
using DevBrain.Domain.Entities;
using DevBrain.Domain.Enums;
using DevBrain.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DevBrain.Infrastructure.Persistence;

public class DevBrainDbContext : DbContext
{
    public DevBrainDbContext(DbContextOptions<DevBrainDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Challenge> Challenges => Set<Challenge>();
    public DbSet<Attempt> Attempts => Set<Attempt>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Users table
        modelBuilder.Entity<User>()
            .ToTable("users")
            .HasKey(u => u.Id);

        modelBuilder.Entity<User>()
            .Property(u => u.Id)
            .ValueGeneratedNever()  // We generate Guid.NewGuid() in domain
            .IsRequired();

        modelBuilder.Entity<User>()
            .Property(u => u.Email)
            .HasMaxLength(255)
            .IsRequired();

        modelBuilder.Entity<User>()
            .Property(u => u.PasswordHash)
            .IsRequired();

        modelBuilder.Entity<User>()
            .Property(u => u.DisplayName)
            .HasMaxLength(50)
            .IsRequired();

        modelBuilder.Entity<User>()
            .Property(u => u.EloRating)
            .IsRequired()
            .HasDefaultValue(1000);

        modelBuilder.Entity<User>()
            .Property(u => u.CreatedAt)
            .IsRequired();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Configure Challenges table
        modelBuilder.Entity<Challenge>()
            .ToTable("challenges")
            .HasKey(c => c.Id);

        modelBuilder.Entity<Challenge>()
            .Property(c => c.Id)
            .IsRequired();

        modelBuilder.Entity<Challenge>()
            .Property(c => c.Title)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<Challenge>()
            .Property(c => c.Description)
            .IsRequired();

        modelBuilder.Entity<Challenge>()
            .Property(c => c.Category)
            .HasColumnName("category")
            .IsRequired();

        modelBuilder.Entity<Challenge>()
            .Property(c => c.Difficulty)
            .HasColumnName("difficulty")
            .IsRequired();

        modelBuilder.Entity<Challenge>()
            .Property(c => c.CorrectAnswer)
            .IsRequired();

        modelBuilder.Entity<Challenge>()
            .Property(c => c.TimeLimitSecs)
            .IsRequired();

        modelBuilder.Entity<Challenge>()
            .Property(c => c.CreatedAt)
            .IsRequired();

        modelBuilder.Entity<Challenge>()
            .Property(c => c.Type)
            .HasColumnName("type")
            .IsRequired()
            .HasDefaultValue(ChallengeType.OpenText);

        modelBuilder.Entity<Challenge>()
            .Property(c => c.Options)
            .HasColumnName("options")
            .HasConversion(
                v => v.Count == 0 ? null : string.Join("|", v),
                v => string.IsNullOrEmpty(v)
                    ? (IReadOnlyList<string>)Array.Empty<string>()
                    : v.Split('|').ToList().AsReadOnly(),
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyList<string>>(
                    (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
                    v => v == null ? 0 : v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
                    v => v == null ? Array.Empty<string>() : v.ToList().AsReadOnly()
                )
            )
            .IsRequired(false);

        modelBuilder.Entity<Challenge>()
            .Property(c => c.StarterCode)
            .HasColumnName("starter_code")
            .IsRequired(false);

        modelBuilder.Entity<Challenge>()
            .Property(c => c.TestCases)
            .HasColumnName("test_cases")
            .HasConversion(
                v => v.Count == 0 ? null : JsonSerializer.Serialize(
                    v.Select(tc => new { tc.Input, tc.ExpectedOutput, tc.Description }),
                    (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v)
                    ? (IReadOnlyList<CodeTestCase>)Array.Empty<CodeTestCase>()
                    : JsonSerializer.Deserialize<List<TestCaseJson>>(v, (JsonSerializerOptions?)null)!
                        .Select(d => CodeTestCase.Create(d.Input, d.ExpectedOutput, d.Description))
                        .ToList()
                        .AsReadOnly(),
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyList<CodeTestCase>>(
                    (a, b) => (a == null && b == null) || (a != null && b != null && a.Count == b.Count
                        && a.Zip(b).All(pair => pair.First.ExpectedOutput == pair.Second.ExpectedOutput
                            && pair.First.Description == pair.Second.Description)),
                    v => v == null ? 0 : v.Aggregate(0, (h, tc) => HashCode.Combine(h, tc.ExpectedOutput.GetHashCode())),
                    v => v == null ? Array.Empty<CodeTestCase>() : v.ToList().AsReadOnly()
                )
            )
            .IsRequired(false);

        modelBuilder.Entity<Challenge>()
            .HasIndex(c => c.Category);

        modelBuilder.Entity<Challenge>()
            .HasIndex(c => c.Difficulty);

        modelBuilder.Entity<Challenge>()
            .HasIndex(c => c.Type);

        // Configure Attempts table
        modelBuilder.Entity<Attempt>()
            .ToTable("attempts")
            .HasKey(a => a.Id);

        modelBuilder.Entity<Attempt>()
            .Property(a => a.Id)
            .IsRequired();

        modelBuilder.Entity<Attempt>()
            .Property(a => a.UserId)
            .IsRequired();  // Changed from HasMaxLength(36)

        modelBuilder.Entity<Attempt>()
            .Property(a => a.ChallengeId)
            .IsRequired();

        modelBuilder.Entity<Attempt>()
            .Property(a => a.UserAnswer)
            .IsRequired();

        modelBuilder.Entity<Attempt>()
            .Property(a => a.IsCorrect)
            .IsRequired();

        modelBuilder.Entity<Attempt>()
            .Property(a => a.ElapsedSecs)
            .IsRequired();

        modelBuilder.Entity<Attempt>()
            .Property(a => a.OccurredAt)
            .IsRequired();

        // Foreign Keys
        modelBuilder.Entity<Attempt>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Attempt>()
            .HasOne(a => a.Challenge)
            .WithMany()
            .HasForeignKey(a => a.ChallengeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        modelBuilder.Entity<Attempt>()
            .HasIndex(a => a.UserId);

        modelBuilder.Entity<Attempt>()
            .HasIndex(new[] { nameof(Attempt.UserId), nameof(Attempt.OccurredAt) })
            .IsDescending(false, true);

        modelBuilder.Entity<Attempt>()
            .HasIndex(new[] { nameof(Attempt.UserId), nameof(Attempt.ChallengeId), nameof(Attempt.IsCorrect) });

        // Configure UserBadges table
        modelBuilder.Entity<UserBadge>()
            .ToTable("user_badges")
            .HasKey(b => b.Id);

        modelBuilder.Entity<UserBadge>()
            .Property(b => b.Id)
            .ValueGeneratedNever();

        modelBuilder.Entity<UserBadge>()
            .Property(b => b.UserId)
            .IsRequired();

        modelBuilder.Entity<UserBadge>()
            .Property(b => b.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        modelBuilder.Entity<UserBadge>()
            .Property(b => b.EarnedAt)
            .IsRequired();

        modelBuilder.Entity<UserBadge>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserBadge>()
            .HasIndex(b => b.UserId);

        modelBuilder.Entity<UserBadge>()
            .HasIndex(new[] { nameof(UserBadge.UserId), nameof(UserBadge.Type) })
            .IsUnique();

        // Seed data - 10 challenges with mix of categories and difficulties
        // Note: DisableAutoTraitoryGenerationSeeding() in test factory should prevent double seeding
        SeedChallenges(modelBuilder);
    }

    private sealed record TestCaseJson(string Input, string ExpectedOutput, string Description);

    private static void SeedChallenges(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var challenges = new[]
        {
            Challenge.CreateForSeeding(
                new Guid("10000000-0000-0000-0000-000000000001"),
                "SQL: Select Top N Records",
                "Write a SQL query that returns the top 5 users by number of completed attempts",
                ChallengeCategory.Sql,
                Difficulty.Easy,
                "SELECT TOP 5 u.id, COUNT(a.id) as attempt_count FROM users u LEFT JOIN attempts a ON u.id = a.user_id GROUP BY u.id ORDER BY attempt_count DESC",
                60,
                seedDate
            ),
            Challenge.CreateForSeeding(
                new Guid("10000000-0000-0000-0000-000000000002"),
                "SQL: Join Multiple Tables",
                "Write a query that joins users with their latest attempt, including the challenge title",
                ChallengeCategory.Sql,
                Difficulty.Medium,
                "SELECT u.id, u.email, c.title, a.created_at FROM users u LEFT JOIN attempts a ON u.id = a.user_id LEFT JOIN challenges c ON a.challenge_id = c.id WHERE a.created_at = (SELECT MAX(created_at) FROM attempts WHERE user_id = u.id)",
                120,
                seedDate
            ),
            Challenge.CreateForSeeding(
                new Guid("10000000-0000-0000-0000-000000000003"),
                "C#: Extract Method",
                "Refactor this code to follow DRY: if (x > 10) print(\"big\"); if (y > 10) print(\"big\");",
                ChallengeCategory.CodeLogic,
                Difficulty.Easy,
                "private void CheckAndPrint(int value) { if (value > 10) Print(\"big\"); }",
                90,
                seedDate
            ),
            Challenge.CreateForSeeding(
                new Guid("10000000-0000-0000-0000-000000000004"),
                "C#: Null Coalescing",
                "What operator returns the first non-null value in C#?",
                ChallengeCategory.CodeLogic,
                Difficulty.Easy,
                "??",
                45,
                seedDate
            ),
            Challenge.CreateForSeeding(
                new Guid("10000000-0000-0000-0000-000000000005"),
                "Architecture: SOLID - Single Responsibility",
                "Which SOLID principle states that a class should have only one reason to change?",
                ChallengeCategory.Architecture,
                Difficulty.Medium,
                "single responsibility principle",
                75,
                seedDate
            ),
            Challenge.CreateForSeeding(
                new Guid("10000000-0000-0000-0000-000000000006"),
                "Architecture: Design Pattern",
                "What design pattern restricts object instantiation to a single instance?",
                ChallengeCategory.Architecture,
                Difficulty.Hard,
                "singleton",
                150,
                seedDate
            ),
            Challenge.CreateForSeeding(
                new Guid("10000000-0000-0000-0000-000000000007"),
                "Docker: Container Listing",
                "What Docker command lists all containers (running and stopped)?",
                ChallengeCategory.DevOps,
                Difficulty.Easy,
                "docker ps -a",
                60,
                seedDate
            ),
            Challenge.CreateForSeeding(
                new Guid("10000000-0000-0000-0000-000000000008"),
                "Docker: Image Cleanup",
                "What Docker command removes all unused images?",
                ChallengeCategory.DevOps,
                Difficulty.Medium,
                "docker image prune",
                90,
                seedDate
            ),
            Challenge.CreateForSeeding(
                new Guid("10000000-0000-0000-0000-000000000009"),
                "Memory: Variable Tracing",
                "Trace this code: x = 5; x += 3; x *= 2; x -= 1; What is the final value of x?",
                ChallengeCategory.WorkingMemory,
                Difficulty.Medium,
                "15",
                120,
                seedDate
            ),
            Challenge.CreateForSeeding(
                new Guid("10000000-0000-0000-0000-000000000010"),
                "Memory: Loop Counting",
                "Count how many times this loop executes: for (int i = 0; i < 20; i += 3) {}",
                ChallengeCategory.WorkingMemory,
                Difficulty.Easy,
                "7",
                60,
                seedDate
            ),
            Challenge.CreateForSeeding(
                new Guid("20000000-0000-0000-0000-000000000001"),
                "Algorithm: Binary Search Complexity",
                "What is the time complexity of binary search on a sorted array of n elements?",
                ChallengeCategory.CodeLogic,
                Difficulty.Medium,
                "O(log n)",
                60,
                seedDate,
                ChallengeType.MultipleChoice,
                new[] { "O(n)", "O(n²)", "O(log n)", "O(n log n)" }
            ),
            Challenge.CreateForSeeding(
                new Guid("20000000-0000-0000-0000-000000000002"),
                "SQL: JOIN Type",
                "Which JOIN returns only rows that have matching values in BOTH tables?",
                ChallengeCategory.Sql,
                Difficulty.Easy,
                "INNER JOIN",
                45,
                seedDate,
                ChallengeType.MultipleChoice,
                new[] { "LEFT JOIN", "RIGHT JOIN", "INNER JOIN", "FULL OUTER JOIN" }
            ),
            Challenge.CreateForSeeding(
                new Guid("20000000-0000-0000-0000-000000000003"),
                "Architecture: Strategy Pattern",
                "Which design pattern defines a family of algorithms, encapsulates each one, and makes them interchangeable?",
                ChallengeCategory.Architecture,
                Difficulty.Medium,
                "Strategy",
                75,
                seedDate,
                ChallengeType.MultipleChoice,
                new[] { "Observer", "Strategy", "Command", "Decorator" }
            ),
            Challenge.CreateForSeeding(
                new Guid("30000000-0000-0000-0000-000000000001"),
                "JS: Sum Two Numbers",
                "Write a JavaScript function `solution(a, b)` that returns the sum of two numbers.",
                ChallengeCategory.CodeLogic,
                Difficulty.Easy,
                "PASS",
                120,
                seedDate,
                ChallengeType.CodeRunner,
                options: null,
                starterCode: "function solution(a, b) {\n  // Write your code here\n}",
                testCases: new[]
                {
                    CodeTestCase.Create("2, 3", "5", "Returns sum of 2 and 3"),
                    CodeTestCase.Create("0, 0", "0", "Returns sum of 0 and 0"),
                    CodeTestCase.Create("-1, 1", "0", "Returns sum of -1 and 1")
                }
            ),
            Challenge.CreateForSeeding(
                new Guid("30000000-0000-0000-0000-000000000002"),
                "JS: Filter Even Numbers",
                "Write a JavaScript function `solution(arr)` that returns a new array containing only the even numbers from the input array.",
                ChallengeCategory.CodeLogic,
                Difficulty.Easy,
                "PASS",
                120,
                seedDate,
                ChallengeType.CodeRunner,
                options: null,
                starterCode: "function solution(arr) {\n  // Write your code here\n}",
                testCases: new[]
                {
                    CodeTestCase.Create("[1, 2, 3, 4]", "2,4", "Filters even numbers from mixed array"),
                    CodeTestCase.Create("[0, 1, 2]", "0,2", "Includes zero as even"),
                    CodeTestCase.Create("[2, 4, 6]", "2,4,6", "Returns all elements when all are even")
                }
            )
        };

        modelBuilder.Entity<Challenge>().HasData(challenges);
    }
}
