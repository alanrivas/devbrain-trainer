using DevBrain.Api;
using DevBrain.Domain.Interfaces;
using DevBrain.Infrastructure.Persistence;
using DevBrain.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace DevBrain.Integration.Tests;

public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;
    private RedisContainer? _redisContainer;
    private string? _postgresConnectionString;
    private string? _redisConnectionString;

    public IntegrationTestFactory()
    {
        // Signal to Program.cs that we're running in a test environment
        // Do this in constructor, BEFORE CreateClient() builds the app
        Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_TEST", "true");
    }

    public async Task InitializeAsync()
    {
        // PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("devbrain_integration_test")
            .WithUsername("testuser")
            .WithPassword("testpass123")
            .Build();

        await _postgresContainer.StartAsync();
        _postgresConnectionString = _postgresContainer.GetConnectionString();

        // Redis container
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        await _redisContainer.StartAsync();
        _redisConnectionString = _redisContainer.GetConnectionString();

        // Run migrations on test DB
        await RunMigrationsAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_postgresContainer != null)
        {
            await _postgresContainer.StopAsync();
            await _postgresContainer.DisposeAsync();
        }

        if (_redisContainer != null)
        {
            await _redisContainer.StopAsync();
            await _redisContainer.DisposeAsync();
        }

        // Also clean up the base WebApplicationFactory resources
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment to Testing so Program.cs doesn't try to use default connection string
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration if it exists (shouldn't, because of Testing env)
            var dbContextDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DevBrainDbContext>));

            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);

            // Add PostgreSQL context pointing to TestContainers
            services.AddDbContext<DevBrainDbContext>(options =>
                options.UseNpgsql(_postgresConnectionString)
            );

            // Remove IConnectionMultiplexer (we'll use mock StreakService instead)
            var redisDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(StackExchange.Redis.IConnectionMultiplexer));

            if (redisDescriptor != null)
                services.Remove(redisDescriptor);

            // Remove existing StreakService registration
            var streakDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(IStreakService));

            if (streakDescriptor != null)
                services.Remove(streakDescriptor);

            // Use MockStreakService for integration tests (no external Redis dependency)
            // Register as Singleton so all scopes share the same in-memory streak state within a test
            services.AddSingleton<MockStreakService>();
            services.AddSingleton<IStreakService>(provider => provider.GetRequiredService<MockStreakService>());
        });
    }

    private async Task RunMigrationsAsync()
    {
        if (_postgresConnectionString == null)
            throw new InvalidOperationException("Connection string not initialized");

        var optionsBuilder = new DbContextOptionsBuilder<DevBrainDbContext>();
        optionsBuilder.UseNpgsql(_postgresConnectionString);

        using var context = new DevBrainDbContext(optionsBuilder.Options);
        
        // Get any pending migrations
        var pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToList();
        System.Diagnostics.Debug.WriteLine($"[RunMigrationsAsync] Pending migrations: {pendingMigrations.Count}");
        foreach (var migration in pendingMigrations)
        {
            System.Diagnostics.Debug.WriteLine($"  - {migration}");
        }
        
        // Run migrations
        await context.Database.MigrateAsync();
        
        // Verify challenges were seeded
        var challengeCount = await context.Challenges.CountAsync();
        System.Diagnostics.Debug.WriteLine($"[RunMigrationsAsync] Challenges after migration: {challengeCount}");
        
        if (challengeCount > 0)
        {
            var firstChallenge = await context.Challenges.FirstAsync();
            System.Diagnostics.Debug.WriteLine($"[RunMigrationsAsync] First challenge: {firstChallenge.Id} - {firstChallenge.Title}");
        }
    }

    public async Task<DevBrainDbContext> GetDbContextAsync()
    {
        var optionsBuilder = new DbContextOptionsBuilder<DevBrainDbContext>();
        optionsBuilder.UseNpgsql(_postgresConnectionString);
        var context = new DevBrainDbContext(optionsBuilder.Options);
        return context;
    }
}
