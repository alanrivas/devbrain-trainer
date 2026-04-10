using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DevBrain.Api.DTOs;
using DevBrain.Api.Services;
using DevBrain.Domain.Entities;

namespace DevBrain.Api.Tests;

public class GetUserStatsTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory = new();
    private HttpClient _client = null!;
    private string _validToken = null!;
    private Guid _userId;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();

        // Create a test user and get a valid JWT token via login
        var db = await _factory.GetDbContextAsync();
        var passwordHashService = new PasswordHashService();
        var passwordHash = passwordHashService.HashPassword("StatsTest123!");
        var user = User.CreateFromRegistration(
            email: "stats-tester@example.com",
            passwordHash: passwordHash,
            displayName: "Stats Tester"
        );
        db.Users.Add(user);
        await db.SaveChangesAsync();
        _userId = user.Id;

        var loginRequest = new { email = "stats-tester@example.com", password = "StatsTest123!" };
        var loginContent = new StringContent(
            JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"
        );
        var loginResponse = await _client.PostAsync("/api/v1/auth/login", loginContent);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        _validToken = loginBody!.Token;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // ─── Helpers ────────────────────────────────────────────────────────────────

    private void SetAuthHeader() =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _validToken);

    private async Task<T?> Deserialize<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private async Task PostAttempt(Guid challengeId, string answer)
    {
        SetAuthHeader();
        var body = new StringContent(
            JsonSerializer.Serialize(new { userAnswer = answer, elapsedSeconds = 30 }),
            Encoding.UTF8, "application/json"
        );
        await _client.PostAsync($"/api/v1/challenges/{challengeId}/attempt", body);
    }

    private async Task<List<ChallengeResponseDto>> GetAllChallenges()
    {
        var response = await _client.GetAsync("/api/v1/challenges?pageSize=50");
        var result = await Deserialize<PaginatedChallengeResponse>(response);
        return result!.Items.ToList();
    }

    private record PaginatedChallengeResponse(IReadOnlyList<ChallengeResponseDto> Items, int TotalCount);

    private record UserStatsResponse(
        Guid UserId,
        string DisplayName,
        int TotalAttempts,
        int CorrectAttempts,
        float AccuracyRate,
        int CurrentStreak,
        int EloRating,
        DateTimeOffset? LastAttemptAt
    );

    // ─── Tests ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserStats_WithoutToken_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/v1/users/me/stats");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserStats_WithNoAttempts_ShouldReturn200WithZeroStats()
    {
        SetAuthHeader();

        var response = await _client.GetAsync("/api/v1/users/me/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await Deserialize<UserStatsResponse>(response);
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalAttempts);
        Assert.Equal(0, result.CorrectAttempts);
        Assert.Equal(0.0f, result.AccuracyRate);
        Assert.Null(result.LastAttemptAt);
    }

    [Fact]
    public async Task GetUserStats_UserIdAndDisplayNameMatchToken()
    {
        SetAuthHeader();

        var response = await _client.GetAsync("/api/v1/users/me/stats");

        var result = await Deserialize<UserStatsResponse>(response);
        Assert.NotNull(result);
        Assert.Equal(_userId, result.UserId);
        Assert.Equal("Stats Tester", result.DisplayName);
    }

    [Fact]
    public async Task GetUserStats_TotalAttemptsCountsAllAttempts()
    {
        var challenges = await GetAllChallenges();
        var challengeId = challenges.First().Id;

        await PostAttempt(challengeId, "WRONG");
        await PostAttempt(challengeId, "WRONG");
        await PostAttempt(challengeId, "WRONG");

        SetAuthHeader();
        var response = await _client.GetAsync("/api/v1/users/me/stats");

        var result = await Deserialize<UserStatsResponse>(response);
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalAttempts);
    }

    [Fact]
    public async Task GetUserStats_CorrectAttemptsOnlyCountsCorrectOnes()
    {
        var testChallenges = CustomWebApplicationFactory.GetTestChallenges();
        var challenges = await GetAllChallenges();
        var first = challenges.First(c => c.Title == testChallenges[0].Title);
        var second = challenges.First(c => c.Title == testChallenges[1].Title);

        await PostAttempt(first.Id, testChallenges[0].CorrectAnswer); // correct
        await PostAttempt(first.Id, "WRONG");                         // incorrect
        await PostAttempt(second.Id, testChallenges[1].CorrectAnswer); // correct

        SetAuthHeader();
        var response = await _client.GetAsync("/api/v1/users/me/stats");

        var result = await Deserialize<UserStatsResponse>(response);
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalAttempts);
        Assert.Equal(2, result.CorrectAttempts);
    }

    [Fact]
    public async Task GetUserStats_AccuracyRate_IsCorrectlyCalculated()
    {
        var testChallenges = CustomWebApplicationFactory.GetTestChallenges();
        var challenges = await GetAllChallenges();
        var first = challenges.First(c => c.Title == testChallenges[0].Title);

        await PostAttempt(first.Id, testChallenges[0].CorrectAnswer); // correct
        await PostAttempt(first.Id, "WRONG");                         // incorrect

        SetAuthHeader();
        var response = await _client.GetAsync("/api/v1/users/me/stats");

        var result = await Deserialize<UserStatsResponse>(response);
        Assert.NotNull(result);
        Assert.Equal(0.5f, result.AccuracyRate);
    }

    [Fact]
    public async Task GetUserStats_AllCorrect_AccuracyRateIsOne()
    {
        var testChallenges = CustomWebApplicationFactory.GetTestChallenges();
        var challenges = await GetAllChallenges();
        var first = challenges.First(c => c.Title == testChallenges[0].Title);

        await PostAttempt(first.Id, testChallenges[0].CorrectAnswer);
        await PostAttempt(first.Id, testChallenges[0].CorrectAnswer);

        SetAuthHeader();
        var response = await _client.GetAsync("/api/v1/users/me/stats");

        var result = await Deserialize<UserStatsResponse>(response);
        Assert.NotNull(result);
        Assert.Equal(1.0f, result.AccuracyRate);
    }

    [Fact]
    public async Task GetUserStats_PlaceholderStreak_AlwaysReturnsZero()
    {
        SetAuthHeader();

        var response = await _client.GetAsync("/api/v1/users/me/stats");

        var result = await Deserialize<UserStatsResponse>(response);
        Assert.NotNull(result);
        Assert.Equal(0, result.CurrentStreak);
    }

    [Fact]
    public async Task GetUserStats_PlaceholderEloRating_AlwaysReturns1000()
    {
        SetAuthHeader();

        var response = await _client.GetAsync("/api/v1/users/me/stats");

        var result = await Deserialize<UserStatsResponse>(response);
        Assert.NotNull(result);
        Assert.Equal(1000, result.EloRating);
    }

    [Fact]
    public async Task GetUserStats_LastAttemptAt_ReflectsTheMostRecentAttempt()
    {
        var challenges = await GetAllChallenges();
        var challengeId = challenges.First().Id;

        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        await PostAttempt(challengeId, "any-answer");
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        SetAuthHeader();
        var response = await _client.GetAsync("/api/v1/users/me/stats");

        var result = await Deserialize<UserStatsResponse>(response);
        Assert.NotNull(result);
        Assert.NotNull(result.LastAttemptAt);
        Assert.True(result.LastAttemptAt >= before);
        Assert.True(result.LastAttemptAt <= after);
    }
}
