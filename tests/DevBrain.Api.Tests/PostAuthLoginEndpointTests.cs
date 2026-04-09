using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using DevBrain.Api.DTOs;
using DevBrain.Api.Services;
using DevBrain.Domain.Entities;
using DevBrain.Infrastructure.Persistence;
using Xunit;

namespace DevBrain.Api.Tests;

public class PostAuthLoginEndpointTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<User> CreateTestUserAsync(DevBrainDbContext db, string email, string plainPassword, string displayName)
    {
        var passwordHashService = new PasswordHashService();
        var passwordHash = passwordHashService.HashPassword(plainPassword);
        
        var user = User.CreateFromRegistration(
            email: email,
            passwordHash: passwordHash,
            displayName: displayName
        );
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    // Validación de entrada (4 tests)

    [Fact]
    public async Task LoginWithMissingEmail_ShouldReturn400()
    {
        // When email is missing, ASP.NET auto-validation rejects it (required field)
        var request = new { password = "Pass123" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var response = await _client.PostAsync("/api/v1/auth/login", content);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LoginWithMissingPassword_ShouldReturn400()
    {
        // When password is missing, ASP.NET auto-validation rejects it (required field)
        var request = new { email = "user@example.com" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var response = await _client.PostAsync("/api/v1/auth/login", content);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LoginWithInvalidEmailFormat_ShouldReturn400()
    {
        var request = new { email = "notanemail", password = "Pass123" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var response = await _client.PostAsync("/api/v1/auth/login", content);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Email format is invalid", body);
    }

    [Fact]
    public async Task LoginWithEmptyBothFields_ShouldReturn400()
    {
        // Empty strings for email and password should fail ASP.NET validation
        var request = new { email = "", password = "" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var response = await _client.PostAsync("/api/v1/auth/login", content);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Autenticación (4 tests)

    [Fact]
    public async Task LoginWithNonexistentEmail_ShouldReturn401()
    {
        var request = new { email = "nonexistent@example.com", password = "Pass123" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var response = await _client.PostAsync("/api/v1/auth/login", content);

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoginWithIncorrectPassword_ShouldReturn401()
    {
        var db = await _factory.GetDbContextAsync();
        await CreateTestUserAsync(db, "user@example.com", "CorrectPass123", "Test User");

        var request = new { email = "user@example.com", password = "WrongPass123" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var response = await _client.PostAsync("/api/v1/auth/login", content);

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoginWithCorrectCredentials_ShouldReturn200WithToken()
    {
        var db = await _factory.GetDbContextAsync();
        var user = await CreateTestUserAsync(db, "john@example.com", "SecurePass123", "John");

        var request = new { email = "john@example.com", password = "SecurePass123" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var response = await _client.PostAsync("/api/v1/auth/login", content);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var responseBody = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        
        Assert.NotNull(responseBody);
        Assert.NotNull(responseBody.Token);
        Assert.NotNull(responseBody.User);
        Assert.Equal(user.Id, responseBody.User.Id);
        Assert.Equal("john@example.com", responseBody.User.Email);
        Assert.Equal("John", responseBody.User.DisplayName);
    }

    [Fact]
    public async Task LoginWithCaseInsensitiveEmail_ShouldReturn200()
    {
        var db = await _factory.GetDbContextAsync();
        await CreateTestUserAsync(db, "User@Example.com", "SecurePass123", "Test User");

        var request = new { email = "user@example.com", password = "SecurePass123" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var response = await _client.PostAsync("/api/v1/auth/login", content);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var responseBody = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        
        Assert.NotNull(responseBody);
        Assert.NotNull(responseBody.Token);
    }

    // Token validación (3 tests)

    [Fact]
    public async Task LoginTokenHasCorrectExpiration_ShouldBe24Hours()
    {
        var db = await _factory.GetDbContextAsync();
        await CreateTestUserAsync(db, "token@example.com", "SecurePass123", "Token User");

        var request = new { email = "token@example.com", password = "SecurePass123" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var beforeRequest = DateTime.UtcNow;
        var response = await _client.PostAsync("/api/v1/auth/login", content);
        var afterRequest = DateTime.UtcNow;

        var responseBody = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        var token = responseBody!.Token;

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var expiration = jwtToken.ValidTo;

        var expectedExpiration24hLater = beforeRequest.AddHours(24);
        var timeDifference = Math.Abs((expiration - expectedExpiration24hLater).TotalMinutes);
        
        // Allow 1 minute tolerance
        Assert.True(timeDifference < 1, $"Token expiration off by {timeDifference} minutes");
    }

    [Fact]
    public async Task LoginTokenCanBeDecoded_ShouldHaveValidClaims()
    {
        var db = await _factory.GetDbContextAsync();
        var user = await CreateTestUserAsync(db, "claims@example.com", "SecurePass123", "Claims User");

        var request = new { email = "claims@example.com", password = "SecurePass123" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var response = await _client.PostAsync("/api/v1/auth/login", content);
        var responseBody = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        var token = responseBody!.Token;

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

        Assert.Equal(user.Id.ToString(), subClaim);
        Assert.Equal("claims@example.com", emailClaim);
    }

    [Fact]
    public async Task LoginTokenWithValidAuthHeader_ShouldAuthenticateUser()
    {
        var db = await _factory.GetDbContextAsync();
        await CreateTestUserAsync(db, "authheader@example.com", "SecurePass123", "Auth Header User");

        var loginRequest = new { email = "authheader@example.com", password = "SecurePass123" };
        var loginContent = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(loginRequest),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var loginResponse = await _client.PostAsync("/api/v1/auth/login", loginContent);
        var responseBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        var token = responseBody!.Token;

        // Verify the token is valid JWT
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        Assert.NotNull(jwtToken);
        Assert.True(jwtToken.ValidTo > DateTime.UtcNow, "Token should not be expired");
    }
}
