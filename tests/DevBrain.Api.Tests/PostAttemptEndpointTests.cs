using System.Net;
using System.Text;
using System.Text.Json;
using DevBrain.Api.DTOs;
using DevBrain.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DevBrain.Api.Tests;

public class PostAttemptEndpointTests : IAsyncLifetime
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
        private Guid _firstChallengeId = Guid.Empty;
        private string _firstChallengeAnswer = string.Empty;
        private Guid _secondChallengeId = Guid.Empty;
        private string _secondChallengeAnswer = string.Empty;

        public async Task InitializeAsync()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();
            
            // Set default header for userId
            _client.DefaultRequestHeaders.Add("X-User-Id", "test_user_123");
            
            // Get challenge IDs from seed data via GET endpoint
            var response = await _client.GetAsync("/api/v1/challenges?pageSize=50");
            var result = await DeserializeResponse<PaginatedResponseDto<ChallengeResponseDto>>(response);
            var challenges = result!.Items.ToList();
            
            Console.WriteLine($"Retrieved {challenges.Count} challenges from API");
            foreach (var c in challenges)
            {
                Console.WriteLine($"  - {c.Title}");
            }
            
            // Get test seed data config from factory
            var testChallenges = CustomWebApplicationFactory.GetTestChallenges();
            
            // Match challenges by title to get IDs and answers
            var firstMatch = challenges.FirstOrDefault(c => c.Title == testChallenges[0].Title);
            if (firstMatch != null)
            {
                _firstChallengeId = firstMatch.Id;
                _firstChallengeAnswer = testChallenges[0].CorrectAnswer;
                Console.WriteLine($"First Challenge ID: {_firstChallengeId}, Answer: '{_firstChallengeAnswer}'");
            }
            else
            {
                throw new InvalidOperationException($"Could not find challenge with title '{testChallenges[0].Title}' in {challenges.Count} challenges");
            }
            
            var secondMatch = challenges.FirstOrDefault(c => c.Title == testChallenges[1].Title);
            if (secondMatch != null)
            {
                _secondChallengeId = secondMatch.Id;
                _secondChallengeAnswer = testChallenges[1].CorrectAnswer;
                Console.WriteLine($"Second Challenge ID: {_secondChallengeId}, Answer: '{_secondChallengeAnswer}'");
            }
            else
            {
                throw new InvalidOperationException($"Could not find challenge with title '{testChallenges[1].Title}' in {challenges.Count} challenges");
            }
        }

        public async Task DisposeAsync()
        {
            _client.Dispose();
            await _factory.DisposeAsync();
        }

    private async Task<T?> DeserializeResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private StringContent CreateRequestContent(CreateAttemptRequestDto dto)
    {
        var json = JsonSerializer.Serialize(dto);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    [Fact]
    public async Task PostAttempt_CorrectAnswer_ShouldReturn201WithIsCorrectTrue()
    {
        var request = new CreateAttemptRequestDto(
            UserAnswer: _firstChallengeAnswer,  // Matches first seeded challenge ("7")
            ElapsedSeconds: 30
        );

        var response = await _client.PostAsync(
            $"/api/v1/challenges/{_firstChallengeId}/attempt",
            CreateRequestContent(request)
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await DeserializeResponse<AttemptResponseDto>(response);
        
        Assert.NotNull(result);
        Assert.True(result.IsCorrect);
        Assert.NotEmpty(result.AttemptId.ToString());
        Assert.Equal(_firstChallengeId.ToString(), result.ChallengeId.ToString());
    }

    [Fact]
    public async Task PostAttempt_IncorrectAnswer_ShouldReturn201WithIsCorrectFalse()
    {
        var request = new CreateAttemptRequestDto(
            UserAnswer: "WRONG",  // Wrong answer
            ElapsedSeconds: 30
        );

        var response = await _client.PostAsync(
            $"/api/v1/challenges/{_firstChallengeId}/attempt",
            CreateRequestContent(request)
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await DeserializeResponse<AttemptResponseDto>(response);
        
        Assert.NotNull(result);
        Assert.False(result.IsCorrect);
    }

    [Fact]
    public async Task PostAttempt_CaseInsensitive_ShouldBeCorrect()
    {
        var request = new CreateAttemptRequestDto(
            UserAnswer: _firstChallengeAnswer.ToUpper(),  // Uppercase version
            ElapsedSeconds: 30
        );

        var response = await _client.PostAsync(
            $"/api/v1/challenges/{_firstChallengeId}/attempt",
            CreateRequestContent(request)
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await DeserializeResponse<AttemptResponseDto>(response);
        
        Assert.NotNull(result);
        Assert.True(result.IsCorrect);
    }

    [Fact]
    public async Task PostAttempt_WithWhitespace_TrimmedAndCompared()
    {
        var request = new CreateAttemptRequestDto(
            UserAnswer: $"  {_firstChallengeAnswer}  ",  // With extra spaces
            ElapsedSeconds: 30
        );

        var response = await _client.PostAsync(
            $"/api/v1/challenges/{_firstChallengeId}/attempt",
            CreateRequestContent(request)
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await DeserializeResponse<AttemptResponseDto>(response);
        
        Assert.NotNull(result);
        Assert.True(result.IsCorrect);
    }

    [Fact]
    public async Task PostAttempt_EmptyAnswer_ShouldReturn400()
    {
        var request = new CreateAttemptRequestDto(
            UserAnswer: "",  // Empty
            ElapsedSeconds: 30
        );

        var response = await _client.PostAsync(
            $"/api/v1/challenges/{_firstChallengeId}/attempt",
            CreateRequestContent(request)
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostAttempt_OnlyWhitespace_ShouldReturn400()
    {
        var request = new CreateAttemptRequestDto(
            UserAnswer: "   ",  // Only spaces
            ElapsedSeconds: 30
        );

        var response = await _client.PostAsync(
            $"/api/v1/challenges/{_firstChallengeId}/attempt",
            CreateRequestContent(request)
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostAttempt_NegativeElapsedTime_ShouldReturn400()
    {
        var request = new CreateAttemptRequestDto(
            UserAnswer: _firstChallengeAnswer,
            ElapsedSeconds: -5  // Negative
        );

        var response = await _client.PostAsync(
            $"/api/v1/challenges/{_firstChallengeId}/attempt",
            CreateRequestContent(request)
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostAttempt_ElapsedTimeExceedsMax_ShouldReturn400()
    {
        var request = new CreateAttemptRequestDto(
            UserAnswer: _firstChallengeAnswer,
            ElapsedSeconds: 3601  // > 3600 (1 hour)
        );

        var response = await _client.PostAsync(
            $"/api/v1/challenges/{_firstChallengeId}/attempt",
            CreateRequestContent(request)
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostAttempt_InvalidJSON_ShouldReturn400()
    {
        var invalidJson = "{ invalid json }";
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync(
            $"/api/v1/challenges/{_firstChallengeId}/attempt",
            content
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostAttempt_ChallengeNotFound_ShouldReturn404()
    {
        var nonExistentId = Guid.NewGuid();
        var request = new CreateAttemptRequestDto(
            UserAnswer: _firstChallengeAnswer,
            ElapsedSeconds: 30
        );

        var response = await _client.PostAsync(
            $"/api/v1/challenges/{nonExistentId}/attempt",
            CreateRequestContent(request)
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostAttempt_ValidRequest_ResponseHasAllFields()
    {
        var request = new CreateAttemptRequestDto(
            UserAnswer: _firstChallengeAnswer,
            ElapsedSeconds: 45
        );

        var response = await _client.PostAsync(
            $"/api/v1/challenges/{_firstChallengeId}/attempt",
            CreateRequestContent(request)
        );

        var result = await DeserializeResponse<AttemptResponseDto>(response);
        
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.AttemptId);
        Assert.NotEqual(Guid.Empty, result.ChallengeId);
        Assert.NotEmpty(result.UserId);
        Assert.NotEmpty(result.UserAnswer);
        Assert.NotEmpty(result.CorrectAnswer);
        Assert.Equal(45, result.ElapsedSeconds);
        Assert.NotEmpty(result.ChallengeTitle);
        Assert.NotEqual(default(DateTime), result.OccurredAt);
    }

    [Fact]
    public async Task PostAttempt_ElapsedTimeWithinLimit_ShouldSucceed()
    {
        var request = new CreateAttemptRequestDto(
            UserAnswer: _firstChallengeAnswer,
            ElapsedSeconds: 30  // Within typical limits
        );

        var response = await _client.PostAsync(
            $"/api/v1/challenges/{_firstChallengeId}/attempt",
            CreateRequestContent(request)
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostAttempt_AllowsMultipleAttemptsForSameChallengeAndUser()
    {
        // First attempt (wrong)
        var request1 = new CreateAttemptRequestDto(
            UserAnswer: "WRONG",
            ElapsedSeconds: 20
        );
        var response1 = await _client.PostAsync(
            $"/api/v1/challenges/{_firstChallengeId}/attempt",
            CreateRequestContent(request1)
        );
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        var result1 = await DeserializeResponse<AttemptResponseDto>(response1);
        Assert.False(result1!.IsCorrect);
        
        // Second attempt (correct)
        var request2 = new CreateAttemptRequestDto(
            UserAnswer: _firstChallengeAnswer,
            ElapsedSeconds: 30
        );
        var response2 = await _client.PostAsync(
            $"/api/v1/challenges/{_firstChallengeId}/attempt",
            CreateRequestContent(request2)
        );
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
        var result2 = await DeserializeResponse<AttemptResponseDto>(response2);
        Assert.True(result2!.IsCorrect);
        
        // Verify different attemptIds
        Assert.NotEqual(result1!.AttemptId, result2!.AttemptId);
    }
}
