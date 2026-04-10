using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DevBrain.Api.DTOs;
using Xunit;

namespace DevBrain.Integration.Tests;

// Type aliases for clarity
using ChallengeListItemDto = ChallengeResponseDto;
using ChallengeDetailDto = ChallengeResponseDto;

public class E2EHappyPathTests : IAsyncLifetime
{
    private IntegrationTestFactory _factory = null!;
    private HttpClient _httpClient = null!;
    private string _jwtToken = null!;
    private Guid _userId;
    private Guid _challengeId;

    public async Task InitializeAsync()
    {
        _factory = new IntegrationTestFactory();
        await _factory.InitializeAsync();
        _httpClient = _factory.CreateClient();

        // Pre-register a test user for subsequent tests
        var registerRequest = new
        {
            email = "e2e-user@example.com",
            password = "E2ETest123!",
            displayName = "E2E Tester"
        };

        var registerContent = new StringContent(
            JsonSerializer.Serialize(registerRequest),
            Encoding.UTF8,
            "application/json"
        );

        var registerResponse = await _httpClient.PostAsync("/api/v1/auth/register", registerContent);
        if (registerResponse.StatusCode != HttpStatusCode.Created)
        {
            var errorContent = await registerResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Failed to register test user: {registerResponse.StatusCode} - {errorContent}"
            );
        }
    }

    public async Task DisposeAsync()
    {
        _httpClient?.Dispose();
        await _factory.DisposeAsync();
    }

    private void SetBearerToken() =>
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _jwtToken);

    private async Task<T?> DeserializeResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        try
        {
            return JsonSerializer.Deserialize<T>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize response as {typeof(T).Name}. Status: {response.StatusCode}\nContent: {content}\nError: {ex.Message}",
                ex
            );
        }
    }

    [Fact]
    public async Task E2E_Register_Login_Challenges_Attempt_Stats_Badges_HappyPath()
    {
        // Step 1: POST /auth/login — obtener JWT
        var loginRequest = new { email = "e2e-user@example.com", password = "E2ETest123!" };
        var loginContent = new StringContent(
            JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"
        );
        var loginResponse = await _httpClient.PostAsync("/api/v1/auth/login", loginContent);

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginDto = await DeserializeResponse<LoginResponseDto>(loginResponse);
        Assert.NotNull(loginDto);
        Assert.NotEmpty(loginDto.Token);
        Assert.NotNull(loginDto.User);
        _jwtToken = loginDto.Token;
        _userId = loginDto.User.Id;

        SetBearerToken();

        // Step 2: GET /api/v1/challenges — obtener lista de challenges
        var challengesResponse = await _httpClient.GetAsync("/api/v1/challenges");

        Assert.Equal(HttpStatusCode.OK, challengesResponse.StatusCode);
        var challengesDto = await DeserializeResponse<PaginatedResponseDto<ChallengeResponseDto>>(challengesResponse);
        Assert.NotNull(challengesDto);
        Assert.NotEmpty(challengesDto.Items);
        Assert.Equal(10, challengesDto.TotalCount); // 10 seeded challenges

        _challengeId = challengesDto.Items.First().Id;

        // Step 3: GET /api/v1/challenges/{id} — obtener detalles sin respuesta correcta
        var challengeResponse = await _httpClient.GetAsync($"/api/v1/challenges/{_challengeId}");

        Assert.Equal(HttpStatusCode.OK, challengeResponse.StatusCode);
        var challengeDto = await DeserializeResponse<ChallengeResponseDto>(challengeResponse);
        Assert.NotNull(challengeDto);
        Assert.Equal(_challengeId, challengeDto.Id);
        Assert.NotNull(challengeDto.Title);
        Assert.NotNull(challengeDto.Description);
        // ChallengeResponseDto never includes CorrectAnswer (filtered at API layer)

        // Step 4: POST /api/v1/challenges/{id}/attempt — enviar respuesta correcta
        // Known correct answer from seed data: "SELECT TOP 5 u.id, COUNT(a.id)..." for first challenge
        var attemptRequest = new
        {
            userAnswer = "SELECT TOP 5 u.id, COUNT(a.id) as attempt_count FROM users u LEFT JOIN attempts a ON u.id = a.user_id GROUP BY u.id ORDER BY attempt_count DESC",
            elapsedSeconds = 45
        };
        var attemptContent = new StringContent(
            JsonSerializer.Serialize(attemptRequest), Encoding.UTF8, "application/json"
        );
        var attemptResponse = await _httpClient.PostAsync(
            $"/api/v1/challenges/{_challengeId}/attempt", attemptContent
        );

        Assert.Equal(HttpStatusCode.Created, attemptResponse.StatusCode);
        var attemptDto = await DeserializeResponse<AttemptResponseDto>(attemptResponse);
        Assert.NotNull(attemptDto);
        Assert.True(attemptDto.IsCorrect);
        Assert.True(attemptDto.NewEloRating > 1000); // Should have bonus
        Assert.Equal(1, attemptDto.NewStreak); // First attempt = streak 1
        Assert.NotNull(attemptDto.NewBadges);
        Assert.Contains("FirstBlood", attemptDto.NewBadges);

        // Step 5: GET /api/v1/users/me/stats — validar estadísticas
        var statsResponse = await _httpClient.GetAsync("/api/v1/users/me/stats");

        Assert.Equal(HttpStatusCode.OK, statsResponse.StatusCode);
        var statsDto = await DeserializeResponse<UserStatsResponseDto>(statsResponse);
        Assert.NotNull(statsDto);
        Assert.Equal(1, statsDto.TotalAttempts);
        Assert.Equal(1, statsDto.CorrectAttempts);
        Assert.True(statsDto.AccuracyRate >= 99f && statsDto.AccuracyRate <= 101f, 
            $"AccuracyRate should be ~100, but was {statsDto.AccuracyRate}");
        Assert.Equal(1, statsDto.CurrentStreak);
        Assert.True(statsDto.EloRating > 1000);

        // Step 6: GET /api/v1/users/me/badges — validar badges
        var badgesResponse = await _httpClient.GetAsync("/api/v1/users/me/badges");

        Assert.Equal(HttpStatusCode.OK, badgesResponse.StatusCode);
        var badges = await DeserializeResponse<List<UserBadgeResponseDto>>(badgesResponse);
        Assert.NotNull(badges);
        Assert.Single(badges); // Only FirstBlood earned
        Assert.Equal("FirstBlood", badges[0].Type);
        Assert.NotEqual(default, badges[0].EarnedAt);

        // Step 7: Verify persistence — Direct DB query
        var db = await _factory.GetDbContextAsync();
        var userFromDb = await db.Users.FindAsync(_userId);
        Assert.NotNull(userFromDb);
        Assert.Equal("e2e-user@example.com", userFromDb.Email);

        var attemptsFromDb = db.Attempts.Where(a => a.UserId == _userId).ToList();
        Assert.Single(attemptsFromDb);
        Assert.True(attemptsFromDb[0].IsCorrect);

        var badgesFromDb = db.UserBadges.Where(b => b.UserId == _userId).ToList();
        Assert.Single(badgesFromDb);
        Assert.Equal("FirstBlood", badgesFromDb[0].Type.ToString());
    }

    [Fact]
    public async Task E2E_MultipleAttempts_SameChallengeByDifferentUsers_NoConflict()
    {
        // Create second user
        var secondUserRegister = new
        {
            email = "e2e-user2@example.com",
            password = "E2ETest123!",
            displayName = "E2E Tester 2"
        };
        var secondUserContent = new StringContent(
            JsonSerializer.Serialize(secondUserRegister), Encoding.UTF8, "application/json"
        );
        var secondUserRegisterResponse = await _httpClient.PostAsync("/api/v1/auth/register", secondUserContent);
        Assert.Equal(HttpStatusCode.Created, secondUserRegisterResponse.StatusCode);

        // Login as second user
        var loginRequest = new { email = "e2e-user2@example.com", password = "E2ETest123!" };
        var loginContent = new StringContent(
            JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"
        );
        var loginResponse = await _httpClient.PostAsync("/api/v1/auth/login", loginContent);
        var loginDto = await DeserializeResponse<LoginResponseDto>(loginResponse);
        var secondUserToken = loginDto!.Token;
        var secondUserId = loginDto.User.Id;

        // Get a challenge
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", secondUserToken);
        var challengesResponse = await _httpClient.GetAsync("/api/v1/challenges");
        var challengesDto = await DeserializeResponse<PaginatedResponseDto<ChallengeResponseDto>>(challengesResponse);
        var challengeId = challengesDto!.Items.First().Id;

        // Both users attempt the same challenge
        var attemptRequest = new
        {
            userAnswer = "SELECT TOP 5 u.id, COUNT(a.id) as attempt_count FROM users u LEFT JOIN attempts a ON u.id = a.user_id GROUP BY u.id ORDER BY attempt_count DESC",
            elapsedSeconds = 45
        };
        var attemptContent = new StringContent(
            JsonSerializer.Serialize(attemptRequest), Encoding.UTF8, "application/json"
        );
        var attemptResponse = await _httpClient.PostAsync($"/api/v1/challenges/{challengeId}/attempt", attemptContent);

        Assert.Equal(HttpStatusCode.Created, attemptResponse.StatusCode);
        var attemptDto = await DeserializeResponse<AttemptResponseDto>(attemptResponse);
        Assert.True(attemptDto!.IsCorrect);

        // Verify both users have their own stats (no cross-contamination)
        var statsResponse = await _httpClient.GetAsync("/api/v1/users/me/stats");
        var statsDto = await DeserializeResponse<UserStatsResponseDto>(statsResponse);
        Assert.Equal(1, statsDto!.TotalAttempts); // Only second user's attempt
        Assert.Equal(1, statsDto.CorrectAttempts);
    }
}

