using DevBrain.Api.Endpoints;
using DevBrain.Domain.Interfaces;
using DevBrain.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add DB context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<DevBrainDbContext>(options =>
        options.UseNpgsql(connectionString)
    );
}
// If no connection string, the factory will inject in-memory for tests

// Register repositories
builder.Services.AddScoped<IChallengeRepository, EFChallengeRepository>();
builder.Services.AddScoped<IAttemptRepository, EFAttemptRepository>();

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

app.Run();
