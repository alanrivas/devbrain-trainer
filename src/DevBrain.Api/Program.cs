using DevBrain.Api.Endpoints;
using DevBrain.Api.Services;
using DevBrain.Domain.Interfaces;
using DevBrain.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "DevBrain Trainer";
        options.Theme = ScalarTheme.DeepSpace;
    });
}

app.UseHttpsRedirection();

// Map endpoints
app.MapChallengeEndpoints();
app.MapAuthEndpoints();

app.Run();
