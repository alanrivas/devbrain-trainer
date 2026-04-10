using System.Net.Http.Json;
using DevBrain.Domain.Entities;
using DevBrain.Domain.Enums;
using DevBrain.Domain.Interfaces;
using DevBrain.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DevBrain.Api.Tests;

/// <summary>
/// Tests for endpoint logging integration.
/// Validates that all endpoints accept ILogger parameter and function correctly.
/// </summary>
public class EndpointLoggingTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointLoggingTests()
    {
        _factory = new WebApplicationFactory<Program>();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DevBrainDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        _factory.Dispose();
    }

    [Fact]
    public async Task GetChallenges_WithLoggingInjected_ShouldReturn200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/challenges");
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task GetChallenge_WithLoggingInjected_ShouldReturn200ForValidId()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IChallengeRepository>();
        
        var challenge = Challenge.Create(
            "Test SQL", 
            "SQL basics", 
            ChallengeCategory.Sql, 
            Difficulty.Easy, 
            "SELECT *", 
            300
        );
        await repo.AddAsync(challenge);

        var response = await client.GetAsync($"/api/v1/challenges/{challenge.Id}");
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task GetChallenge_WithLoggingInjected_ShouldReturn404ForInvalidId()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/v1/challenges/{Guid.NewGuid()}");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostRegister_WithLoggingInjected_ShouldReturn201()
    {
        var client = _factory.CreateClient();
        var request = new
        {
            email = $"test{Guid.NewGuid():N}@test.com",
            password = "SecurePass123!",
            displayName = "Test User"
        };

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostRegister_WithLoggingInjected_ShouldReturn409ForDuplicate()
    {
        var client = _factory.CreateClient();
        var email = $"dup{Guid.NewGuid():N}@test.com";
        var request = new
        {
            email = email,
            password = "SecurePass123!",
            displayName = "User"
        };

        await client.PostAsJsonAsync("/api/v1/auth/register", request);
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);
        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostLogin_WithLoggingInjected_ShouldReturn200WithToken()
    {
        var client = _factory.CreateClient();
        var email = $"login{Guid.NewGuid():N}@test.com";
        
        var regReq = new
        {
            email = email,
            password = "SecurePass123!",
            displayName = "User"
        };
        await client.PostAsJsonAsync("/api/v1/auth/register", regReq);

        var loginReq = new { email = email, password = "SecurePass123!" };
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task PostLogin_WithLoggingInjected_ShouldReturn401ForWrongPassword()
    {
        var client = _factory.CreateClient();
        var email = $"wrong{Guid.NewGuid():N}@test.com";
        
        var regReq = new
        {
            email = email,
            password = "SecurePass123!",
            displayName = "User"
        };
        await client.PostAsJsonAsync("/api/v1/auth/register", regReq);

        var loginReq = new { email = email, password = "WrongPass123!" };
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserStats_WithLoggingInjected_ShouldReturn200WhenAuthorized()
    {
        var client = _factory.CreateClient();
        var email = $"stats{Guid.NewGuid():N}@test.com";
        
        var regReq = new { email = email, password = "Pass123!", displayName = "User" };
        await client.PostAsJsonAsync("/api/v1/auth/register", regReq);

        var loginReq = new { email = email, password = "Pass123!" };
        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        var content = await loginResp.Content.ReadFromJsonAsync<dynamic>();
        var token = (string)(content?.token ?? content?.data?.token ?? "");

        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/users/me/stats");
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task GetUserBadges_WithLoggingInjected_ShouldReturn200WhenAuthorized()
    {
        var client = _factory.CreateClient();
        var email = $"badge{Guid.NewGuid():N}@test.com";
        
        var regReq = new { email = email, password = "Pass123!", displayName = "User" };
        await client.PostAsJsonAsync("/api/v1/auth/register", regReq);

        var loginReq = new { email = email, password = "Pass123!" };
        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        var content = await loginResp.Content.ReadFromJsonAsync<dynamic>();
        var token = (string)(content?.token ?? content?.data?.token ?? "");

        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/users/me/badges");
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task PostAttempt_WithLoggingInjected_ShouldReturn201WithCorrectAnswer()
    {
        var client = _factory.CreateClient();
        var email = $"attempt{Guid.NewGuid():N}@test.com";
        
        var regReq = new { email = email, password = "Pass123!", displayName = "User" };
        await client.PostAsJsonAsync("/api/v1/auth/register", regReq);

        var loginReq = new { email = email, password = "Pass123!" };
        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        var content = await loginResp.Content.ReadFromJsonAsync<dynamic>();
        var token = (string)(content?.token ?? content?.data?.token ?? "");

        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IChallengeRepository>();
        var challenge = Challenge.Create(
            "Attempt Test",
            "Test",
            ChallengeCategory.Sql,
            Difficulty.Easy,
            "answer",
            300
        );
        await repo.AddAsync(challenge);

        var attemptReq = new { userAnswer = "answer", elapsedSeconds = 10 };
        var response = await client.PostAsJsonAsync($"/api/v1/challenges/{challenge.Id}/attempt", attemptReq);
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    }
}
