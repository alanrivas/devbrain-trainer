using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DevBrain.Api.DTOs;
using DevBrain.Api.Services;
using DevBrain.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevBrain.Api.Tests;

public class GetUserAttemptsTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory = new();
    private HttpClient _client = null!;
    private string _validToken = null!;
    private Guid _userId;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();

        var db = await _factory.GetDbContextAsync();
        var passwordHashService = new PasswordHashService();
        var passwordHash = passwordHashService.HashPassword("AttemptsTest123!");
        var user = User.CreateFromRegistration(
            email: "attempts-tester@example.com",
            passwordHash: passwordHash,
            displayName: "Attempts Tester"
        );
        db.Users.Add(user);
        await db.SaveChangesAsync();
        _userId = user.Id;

        var loginRequest = new { email = "attempts-tester@example.com", password = "AttemptsTest123!" };
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

    private void SetAuthHeader() =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _validToken);

    private async Task<T?> Deserialize<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private async Task<List<ChallengeResponseDto>> GetAllChallenges()
    {
        var response = await _client.GetAsync("/api/v1/challenges?pageSize=50");
        var result = await Deserialize<PaginatedChallengeResponse>(response);
        return result!.Items.ToList();
    }

    private async Task PostAttempt(Guid challengeId, string answer, int elapsedSeconds = 30)
    {
        SetAuthHeader();
        var body = new StringContent(
            JsonSerializer.Serialize(new { userAnswer = answer, elapsedSeconds }),
            Encoding.UTF8, "application/json"
        );
        await _client.PostAsync($"/api/v1/challenges/{challengeId}/attempt", body);
    }

    private record PaginatedChallengeResponse(IReadOnlyList<ChallengeResponseDto> Items, int TotalCount);

    private record AttemptHistoryItemResponse(
        Guid AttemptId,
        Guid ChallengeId,
        string ChallengeTitle,
        bool IsCorrect,
        int ElapsedSecs,
        DateTimeOffset OccurredAt
    );

    [Fact]
    public async Task GetUserAttempts_WithoutToken_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/v1/users/me/attempts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserAttempts_WithNoAttempts_ShouldReturn200WithEmptyArray()
    {
        SetAuthHeader();

        var response = await _client.GetAsync("/api/v1/users/me/attempts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await Deserialize<List<AttemptHistoryItemResponse>>(response);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUserAttempts_WithAttempts_ShouldReturnChallengeTitles()
    {
        var testChallenges = CustomWebApplicationFactory.GetTestChallenges();
        var challenges = await GetAllChallenges();
        var first = challenges.First(c => c.Title == testChallenges[0].Title);

        await PostAttempt(first.Id, testChallenges[0].CorrectAnswer, elapsedSeconds: 42);

        SetAuthHeader();
        var response = await _client.GetAsync("/api/v1/users/me/attempts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await Deserialize<List<AttemptHistoryItemResponse>>(response);
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(first.Id, result[0].ChallengeId);
        Assert.Equal(first.Title, result[0].ChallengeTitle);
        Assert.True(result[0].IsCorrect);
        Assert.Equal(42, result[0].ElapsedSecs);
    }

    [Fact]
    public async Task GetUserAttempts_ShouldReturnMostRecentAttemptFirst()
    {
        var testChallenges = CustomWebApplicationFactory.GetTestChallenges();
        var challenges = await GetAllChallenges();
        var first = challenges.First(c => c.Title == testChallenges[0].Title);
        var second = challenges.First(c => c.Title == testChallenges[1].Title);

        await PostAttempt(first.Id, testChallenges[0].CorrectAnswer, elapsedSeconds: 10);
        await Task.Delay(20);
        await PostAttempt(second.Id, "WRONG", elapsedSeconds: 25);

        SetAuthHeader();
        var response = await _client.GetAsync("/api/v1/users/me/attempts");

        var result = await Deserialize<List<AttemptHistoryItemResponse>>(response);
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(second.Id, result[0].ChallengeId);
        Assert.Equal(first.Id, result[1].ChallengeId);
        Assert.True(result[0].OccurredAt >= result[1].OccurredAt);
    }

    [Fact]
    public async Task GetUserAttempts_ShouldOnlyReturnAuthenticatedUsersAttempts()
    {
        var testChallenges = CustomWebApplicationFactory.GetTestChallenges();
        var challenges = await GetAllChallenges();
        var first = challenges.First(c => c.Title == testChallenges[0].Title);
        var second = challenges.First(c => c.Title == testChallenges[1].Title);

        await PostAttempt(first.Id, testChallenges[0].CorrectAnswer, elapsedSeconds: 15);

        var db = await _factory.GetDbContextAsync();
        var otherPasswordHash = new PasswordHashService().HashPassword("OtherUser123!");
        var otherUser = User.CreateFromRegistration(
            email: "other-attempts@example.com",
            passwordHash: otherPasswordHash,
            displayName: "Other Attempts User"
        );
        db.Users.Add(otherUser);
        await db.SaveChangesAsync();

        var otherChallenge = challenges.First(c => c.Title == testChallenges[1].Title);
        var otherChallengeEntity = await db.Challenges.FirstAsync(c => c.Id == otherChallenge.Id);
        var otherAttempt = Attempt.Create(otherChallengeEntity.Id, otherUser.Id, otherChallengeEntity.CorrectAnswer, 18, otherChallengeEntity);
        db.Attempts.Add(otherAttempt);
        await db.SaveChangesAsync();

        SetAuthHeader();
        var response = await _client.GetAsync("/api/v1/users/me/attempts");

        var result = await Deserialize<List<AttemptHistoryItemResponse>>(response);
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(first.Id, result[0].ChallengeId);
        Assert.DoesNotContain(result, attempt => attempt.ChallengeId == otherChallenge.Id);
    }
}