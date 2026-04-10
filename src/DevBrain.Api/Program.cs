using System.Text;
using DevBrain.Api.Endpoints;
using DevBrain.Api.Services;
using DevBrain.Domain.Interfaces;
using DevBrain.Domain.Services;
using DevBrain.Infrastructure.Persistence;
using DevBrain.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add DB context
// Only use PostgreSQL if:
// 1. Connection string exists in config
// 2. NOT running under test/xUnit (which will inject In-Memory via WebApplicationFactory)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var isTestEnvironment = builder.Environment.EnvironmentName == "Testing" || 
                       Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_TEST") == "true";

if (!string.IsNullOrEmpty(connectionString) && !isTestEnvironment)
{
    builder.Services.AddDbContext<DevBrainDbContext>(options =>
        options.UseNpgsql(connectionString)
    );
}
// Otherwise, the factory will inject in-memory for tests

// Register repositories
builder.Services.AddScoped<IChallengeRepository, EFChallengeRepository>();
builder.Services.AddScoped<IAttemptRepository, EFAttemptRepository>();
builder.Services.AddScoped<IUserRepository, EFUserRepository>();

// Register services
builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IEloRatingService, EloRatingService>();
builder.Services.AddScoped<IAttemptService, AttemptService>();

// Redis + Streak
var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
if (!isTestEnvironment)
{
    try
    {
        var redis = ConnectionMultiplexer.Connect(redisConnectionString);
        builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
        builder.Services.AddScoped<IStreakService, RedisStreakService>();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[STARTUP] Redis connection failed: {ex.Message}. Streak service disabled.");
    }
}

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret not configured");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, _) =>
    {
        document.Info.Title = "DevBrain Trainer API";
        document.Info.Version = "v1";
        document.Info.Description = "API de entrenamiento cognitivo gamificada para desarrolladores.";
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Auto-migrate on startup (production + local, not in tests)
if (!isTestEnvironment && !string.IsNullOrEmpty(connectionString))
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DevBrainDbContext>();
        await db.Database.MigrateAsync();
        Console.WriteLine("[STARTUP] Database migration completed.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[STARTUP] Database migration failed: {ex.Message}");
    }
}

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "ok", timestamp = DateTimeOffset.UtcNow }));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "DevBrain Trainer";
        options.Theme = ScalarTheme.DeepSpace;
    });
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapChallengeEndpoints();
app.MapAuthEndpoints();
app.MapUserEndpoints();

app.Run();
