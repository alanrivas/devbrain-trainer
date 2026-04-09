using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using DevBrain.Api.DTOs;
using DevBrain.Api.Services;
using DevBrain.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace DevBrain.Api.Tests;

public class JwtMiddlewareTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory = new();
    private HttpClient _client = null!;
    private string _validToken = null!;
    private Guid _challengeId;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();

        // Create a test user and get a valid JWT token via login
        var db = await _factory.GetDbContextAsync();
        var passwordHashService = new PasswordHashService();
        var passwordHash = passwordHashService.HashPassword("JwtTest123!");
        var user = User.CreateFromRegistration(
            email: "jwt-tester@example.com",
            passwordHash: passwordHash,
            displayName: "JWT Tester"
        );
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var loginRequest = new { email = "jwt-tester@example.com", password = "JwtTest123!" };
        var loginContent = new StringContent(
            JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"
        );
        var loginResponse = await _client.PostAsync("/api/v1/auth/login", loginContent);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        _validToken = loginBody!.Token;

        // Get a challenge ID from the public endpoint
        var challengesResponse = await _client.GetAsync("/api/v1/challenges");
        var challenges = await challengesResponse.Content.ReadFromJsonAsync<PaginatedChallengeResponse>();
        _challengeId = challenges!.Items.First().Id;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // Helper to build attempt request body
    private static StringContent AttemptBody() =>
        new StringContent(
            JsonSerializer.Serialize(new { userAnswer = "test-answer", elapsedSeconds = 30 }),
            Encoding.UTF8, "application/json"
        );

    // Helper to build a token with a custom secret (simulates wrong-secret token)
    private static string BuildTokenWithSecret(Guid userId, string email, string secret, int expirationHours = 24)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
        };
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expirationHours),
            signingCredentials: credentials
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // --- Endpoints públicos no afectados (2 tests) ---

    [Fact]
    public async Task GetChallenges_WithoutToken_ShouldReturn200()
    {
        var response = await _client.GetAsync("/api/v1/challenges");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetChallengeById_WithoutToken_ShouldReturn200()
    {
        var response = await _client.GetAsync($"/api/v1/challenges/{_challengeId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- Endpoint protegido sin token (2 tests) ---

    [Fact]
    public async Task PostAttempt_WithoutAuthorizationHeader_ShouldReturn401()
    {
        var response = await _client.PostAsync(
            $"/api/v1/challenges/{_challengeId}/attempt",
            AttemptBody()
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostAttempt_WithEmptyBearerToken_ShouldReturn401()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "");

        var response = await _client.PostAsync(
            $"/api/v1/challenges/{_challengeId}/attempt",
            AttemptBody()
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- Endpoint protegido con token inválido (3 tests) ---

    [Fact]
    public async Task PostAttempt_WithMalformedToken_ShouldReturn401()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "this-is-not-a-jwt");

        var response = await _client.PostAsync(
            $"/api/v1/challenges/{_challengeId}/attempt",
            AttemptBody()
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostAttempt_WithTokenSignedWithWrongSecret_ShouldReturn401()
    {
        var wrongSecretToken = BuildTokenWithSecret(
            Guid.NewGuid(),
            "fake@example.com",
            secret: "a-completely-different-secret-key-that-is-wrong-!"
        );
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", wrongSecretToken);

        var response = await _client.PostAsync(
            $"/api/v1/challenges/{_challengeId}/attempt",
            AttemptBody()
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostAttempt_WithExpiredToken_ShouldReturn401()
    {
        // Build a token using the real secret but with negative expiration
        const string realSecret = "your-super-secret-key-min-32-characters-long-change-in-production";
        var expiredToken = BuildTokenWithSecret(
            Guid.NewGuid(),
            "expired@example.com",
            secret: realSecret,
            expirationHours: -1
        );
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await _client.PostAsync(
            $"/api/v1/challenges/{_challengeId}/attempt",
            AttemptBody()
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- Endpoint protegido con token válido (2 tests) ---

    [Fact]
    public async Task PostAttempt_WithValidToken_ShouldNotReturn401()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _validToken);

        var response = await _client.PostAsync(
            $"/api/v1/challenges/{_challengeId}/attempt",
            AttemptBody()
        );

        // The request passed authentication — handler ran (201 Created)
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostAttempt_WithValidToken_ClaimsAvailableInHandler()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _validToken);

        // Read userId from the token to compare against the response
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(_validToken);
        var expectedUserId = Guid.Parse(
            jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value
        );

        var response = await _client.PostAsync(
            $"/api/v1/challenges/{_challengeId}/attempt",
            AttemptBody()
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AttemptResponseDto>();
        Assert.NotNull(body);
        Assert.Equal(expectedUserId, body.UserId);
    }

    // Helper record for deserializing paginated challenge list
    private record PaginatedChallengeResponse(
        IReadOnlyList<ChallengeResponseDto> Items,
        int TotalCount
    );
}
