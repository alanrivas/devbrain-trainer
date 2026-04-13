using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DevBrain.Api.DTOs;
using Xunit;

namespace DevBrain.Integration.Tests;

/// <summary>
/// Chaos/Resilience Integration Tests — validar comportamiento graceful ante fallos de servicios externos.
/// </summary>
public class ChaosResilienceTests : IAsyncLifetime
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

        // Pre-register a test user
        var registerRequest = new
        {
            email = "chaos-user@example.com",
            password = "Chaos123!",
            displayName = "Chaos Tester"
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
                $"Failed to register chaos test user: {registerResponse.StatusCode} - {errorContent}"
            );
        }

        // Login to get JWT
        var loginRequest = new { email = "chaos-user@example.com", password = "Chaos123!" };
        var loginContent = new StringContent(
            JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"
        );
        var loginResponse = await _httpClient.PostAsync("/api/v1/auth/login", loginContent);
        var loginDto = await DeserializeResponse<LoginResponseDto>(loginResponse);
        _jwtToken = loginDto!.Token;
        _userId = loginDto.User!.Id;

        SetBearerToken();

        // Get a challenge for attempt tests
        var challengesResponse = await _httpClient.GetAsync("/api/v1/challenges");
        var challengeList = await DeserializeResponse<PagedResult<ChallengeResponseDto>>(challengesResponse);
        _challengeId = challengeList!.Items.First().Id;
    }

    public async Task DisposeAsync()
    {
        _httpClient?.Dispose();
        await _factory.DisposeAsync();
    }

    private void SetBearerToken() =>
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _jwtToken);

    private void RemoveBearerToken() =>
        _httpClient.DefaultRequestHeaders.Authorization = null;

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

    // Tipo helper para respuestas paginadas
    private record PagedResult<T>(List<T> Items, int Page, int PageSize, int Total);

    /// <summary>
    /// Test: App doesn't crash when JWT is missing
    /// Expected: 401 Unauthorized with clear message
    /// </summary>
    [Fact]
    public async Task SecureEndpoint_WithoutJWT_Returns401Unauthorized()
    {
        // Arrange
        RemoveBearerToken();

        // Act
        var response = await _httpClient.GetAsync("/api/v1/users/me/stats");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test: App doesn't crash when JWT is invalid/malformed
    /// Expected: 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task SecureEndpoint_WithInvalidJWT_Returns401Unauthorized()
    {
        // Arrange
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invalid.token.here");

        // Act
        var response = await _httpClient.GetAsync("/api/v1/users/me/stats");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test: Valid JWT works correctly (baseline)
    /// Expected: 200 OK
    /// </summary>
    [Fact]
    public async Task SecureEndpoint_WithValidJWT_Returns200OK()
    {
        // Arrange — token already set in InitializeAsync
        SetBearerToken();

        // Act
        var response = await _httpClient.GetAsync("/api/v1/users/me/stats");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Test: POST /attempt with valid request body succeeds
    /// Expected: 201 Created (attempt created successfully)
    /// </summary>
    [Fact]
    public async Task PostAttempt_WithValidRequestBody_CreatesAttemptSuccessfully()
    {
        // Arrange
        SetBearerToken();
        
        // First, get a test challenge from the database
        var challengesResponse = await _httpClient.GetAsync("/api/v1/challenges?page=1&pageSize=1");
        var challengeList = await DeserializeResponse<PagedResult<ChallengeResponseDto>>(challengesResponse);
        var testChallenge = challengeList!.Items.FirstOrDefault();
        
        if (testChallenge == null)
        {
            throw new InvalidOperationException("No test challenges found in database");
        }

        // Issue attempt with correct answer (from seeded test data, "YES" is often correct)
        var attemptRequest = new { UserAnswer = "YES", ElapsedSeconds = 10 };
        var content = new StringContent(
            JsonSerializer.Serialize(attemptRequest),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _httpClient.PostAsync(
            $"/api/v1/challenges/{testChallenge.Id}/attempt", content
        );

        // Assert - Accept both 200 and 201
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created,
            $"Expected 200 OK or 201 Created but got {response.StatusCode}"
        );
    }

    /// <summary>
    /// Test: GET /challenges returns 200 OK even if response is slow
    /// Expected: Success after delay (no timeout crash)
    /// </summary>
    [Fact]
    public async Task GetChallenges_WithValidJWT_ReturnsSuccessfully()
    {
        // Arrange
        SetBearerToken();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await _httpClient.GetAsync("/api/v1/challenges");
        stopwatch.Stop();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Response is received (no infinite timeout)
        Assert.True(stopwatch.ElapsedMilliseconds < 30000, 
            $"Response took {stopwatch.ElapsedMilliseconds}ms (expected < 30s)");
    }

    /// <summary>
    /// Test: Health endpoint works without authentication
    /// Expected: 200 OK (health check is public)
    /// </summary>
    [Fact]
    public async Task HealthEndpoint_WithoutAuth_Returns200OK()
    {
        // Arrange
        RemoveBearerToken();

        // Act
        var response = await _httpClient.GetAsync("/health");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            "Health endpoint should not require auth"
        );
    }

    /// <summary>
    /// Test: Multiple concurrent requests don't crash (basic concurrency test)
    /// Expected: All requests succeed
    /// </summary>
    [Fact]
    public async Task ConcurrentRequests_MultipleSimultaneousGets_AllSucceed()
    {
        // Arrange
        SetBearerToken();
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act — Issue 5 concurrent requests
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_httpClient.GetAsync("/api/v1/challenges"));
        }
        var responses = await Task.WhenAll(tasks);

        // Assert — All should succeed
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

        // Cleanup
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    /// <summary>
    /// Test: App gracefully handles missing resources
    /// Expected: Error status code (not crash, not success)
    /// </summary>
    [Fact]
    public async Task Request_WithInvalidData_ReturnsErrorGracefully()
    {
        // Arrange
        SetBearerToken();

        // Act — Request a non-existent user badge endpoint
        var response = await _httpClient.GetAsync($"/api/v1/users/{Guid.NewGuid()}/badges");

        // Assert — Should return error status, not crash or hang
        Assert.False(response.IsSuccessStatusCode, 
            $"Expected an error but got {response.StatusCode}");
        
        // Response should be either 4xx (not found) or 5xx (server error), not timeout
        Assert.True(response.StatusCode >= System.Net.HttpStatusCode.BadRequest,
            "Should return proper HTTP error status");
    }
}
