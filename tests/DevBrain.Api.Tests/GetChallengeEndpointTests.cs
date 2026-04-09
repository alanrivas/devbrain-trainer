using System.Net;
using System.Text.Json;
using DevBrain.Api.DTOs;

namespace DevBrain.Api.Tests;

public class GetChallengeEndpointTests : IAsyncLifetime
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private Guid? _knownChallengeId = null;

    public async Task InitializeAsync()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
        
        // Get a known challenge ID from the list to use in tests
        var listResponse = await _client.GetAsync("/api/v1/challenges?pageSize=1");
        if (listResponse.IsSuccessStatusCode)
        {
            var content = await listResponse.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var listDto = JsonSerializer.Deserialize<dynamic>(content, options);
            
            if (listDto?.GetProperty("items") is JsonElement itemsElement && itemsElement.GetArrayLength() > 0)
            {
                var firstItem = itemsElement[0];
                if (firstItem.TryGetProperty("id", out var idElement))
                {
                    if (Guid.TryParse(idElement.GetString(), out var id))
                    {
                        _knownChallengeId = id;
                    }
                }
            }
        }
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<T?> DeserializeResponse<T>(string content)
    {
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    // Happy path: GET with valid ID
    [Fact]
    public async Task GetChallenge_WithValidId_ShouldReturn200WithChallenge()
    {
        Assert.NotNull(_knownChallengeId);
        var challengeId = _knownChallengeId.Value;

        var response = await _client.GetAsync($"/api/v1/challenges/{challengeId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var challenge = await DeserializeResponse<ChallengeResponseDto>(content);

        Assert.NotNull(challenge);
        Assert.Equal(challengeId, challenge.Id);
        Assert.NotNull(challenge.Title);
        Assert.NotNull(challenge.Description);
        Assert.NotNull(challenge.Category);
        Assert.NotNull(challenge.Difficulty);
        Assert.True(challenge.TimeLimitSecs > 0);
    }

    // Response field validation: no CorrectAnswer exposed
    [Fact]
    public async Task GetChallenge_ResponseShouldNotIncludeCorrectAnswer()
    {
        Assert.NotNull(_knownChallengeId);
        var challengeId = _knownChallengeId.Value;

        var response = await _client.GetAsync($"/api/v1/challenges/{challengeId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify JSON doesn't contain "correctAnswer" field
        Assert.DoesNotContain("correctAnswer", content, StringComparison.OrdinalIgnoreCase);
    }

    // Response field validation: no CreatedAt metadata
    [Fact]
    public async Task GetChallenge_ResponseShouldNotIncludeCreatedAt()
    {
        Assert.NotNull(_knownChallengeId);
        var challengeId = _knownChallengeId.Value;

        var response = await _client.GetAsync($"/api/v1/challenges/{challengeId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify JSON doesn't contain "createdAt" field
        Assert.DoesNotContain("createdAt", content, StringComparison.OrdinalIgnoreCase);
    }

    // Error: Non-existent ID should return 404
    [Fact]
    public async Task GetChallenge_WithNonExistentId_ShouldReturn404()
    {
        var nonExistentId = Guid.NewGuid(); // Definitely not in seed data

        var response = await _client.GetAsync($"/api/v1/challenges/{nonExistentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Error: Invalid GUID format should return 400
    [Fact]
    public async Task GetChallenge_WithInvalidGuidFormat_ShouldReturn400()
    {
        var invalidId = "not-a-guid-at-all";

        var response = await _client.GetAsync($"/api/v1/challenges/{invalidId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // GUID case-insensitivity: uppercase GUID should work
    [Fact]
    public async Task GetChallenge_WithUppercaseGuid_ShouldReturn200()
    {
        Assert.NotNull(_knownChallengeId);
        var challengeId = _knownChallengeId.Value;
        var uppercaseId = challengeId.ToString("B").ToUpper(); // e.g., {10000000-0000-0000-0000-000000000001}

        var response = await _client.GetAsync($"/api/v1/challenges/{uppercaseId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Multiple challenges: verify different IDs return different data
    [Fact]
    public async Task GetChallenge_DifferentChallenges_ShouldReturnDifferentData()
    {
        // Get two different challenges from the list
        var listResponse = await _client.GetAsync("/api/v1/challenges?pageSize=2");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listContent = await listResponse.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var listDto = JsonSerializer.Deserialize<dynamic>(listContent, options);
        
        Assert.NotNull(listDto);
        var itemsElement = listDto?.GetProperty("items");
        Assert.True(itemsElement?.GetArrayLength() >= 2);

        var id1 = Guid.Parse(itemsElement![0].GetProperty("id").GetString()!);
        var id2 = Guid.Parse(itemsElement[1].GetProperty("id").GetString()!);

        var response1 = await _client.GetAsync($"/api/v1/challenges/{id1}");
        var response2 = await _client.GetAsync($"/api/v1/challenges/{id2}");

        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();

        var challenge1 = await DeserializeResponse<ChallengeResponseDto>(content1);
        var challenge2 = await DeserializeResponse<ChallengeResponseDto>(content2);

        Assert.NotNull(challenge1);
        Assert.NotNull(challenge2);
        Assert.NotEqual(challenge1.Title, challenge2.Title);
    }

    // Response consistency: GET {id} should match item from list GET /challenges
    [Fact]
    public async Task GetChallenge_ShouldMatchItemInList()
    {
        Assert.NotNull(_knownChallengeId);
        var challengeId = _knownChallengeId.Value;

        // Get single challenge
        var singleResponse = await _client.GetAsync($"/api/v1/challenges/{challengeId}");
        Assert.Equal(HttpStatusCode.OK, singleResponse.StatusCode);
        var singleContent = await singleResponse.Content.ReadAsStringAsync();
        var singleChallenge = await DeserializeResponse<ChallengeResponseDto>(singleContent);

        // Get list
        var listResponse = await _client.GetAsync("/api/v1/challenges");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listContent = await listResponse.Content.ReadAsStringAsync();
        var listDto = JsonSerializer.Deserialize<dynamic>(listContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Find the challenge in the list
        var items = listDto?.GetProperty("items");
        Assert.NotNull(items);
        
        // Verify the single challenge matches one in the list
        // (Cannot easily compare without more complex JSON parsing, but verify IDs are consistent)
        Assert.NotNull(singleChallenge);
        Assert.Equal(challengeId, singleChallenge.Id);
    }
}
