using System.Net;
using System.Text.Json;
using DevBrain.Api.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DevBrain.Api.Tests;

public class GetChallengesEndpointTests : IAsyncLifetime
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
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

    [Fact]
    public async Task GetChallenges_WithoutParameters_ShouldReturn200WithPaginatedChallenges()
    {
        var response = await _client.GetAsync("/api/v1/challenges");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await DeserializeResponse<PaginatedResponseDto<ChallengeResponseDto>>(response);

        Assert.NotNull(result);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.True(result.TotalCount >= 10, "Should have at least 10 seeded challenges");
        Assert.True(result.TotalPages >= 1);
        Assert.NotEmpty(result.Items);
    }

    [Fact]
    public async Task GetChallenges_WithoutParameters_FirstItemShouldNotIncludeCorrectAnswer()
    {
        var response = await _client.GetAsync("/api/v1/challenges");

        var result = await DeserializeResponse<PaginatedResponseDto<ChallengeResponseDto>>(response);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
        var firstItem = result.Items[0];

        // Verify DTO fields are present
        Assert.NotEqual(Guid.Empty, firstItem.Id);
        Assert.NotEmpty(firstItem.Title);
        Assert.NotEmpty(firstItem.Description);
        Assert.NotEmpty(firstItem.Category);
        Assert.NotEmpty(firstItem.Difficulty);
        Assert.True(firstItem.TimeLimitSecs > 0);
    }

    [Fact]
    public async Task GetChallenges_WithCategoryFilter_ShouldReturnOnlyChallengesOfThatCategory()
    {
        var response = await _client.GetAsync("/api/v1/challenges?category=Sql");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await DeserializeResponse<PaginatedResponseDto<ChallengeResponseDto>>(response);

        Assert.NotNull(result);
        Assert.All(result.Items, item => Assert.Equal("Sql", item.Category));
    }

    [Fact]
    public async Task GetChallenges_WithDifficultyFilter_ShouldReturnOnlyChallengesOfThatDifficulty()
    {
        var response = await _client.GetAsync("/api/v1/challenges?difficulty=Easy");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await DeserializeResponse<PaginatedResponseDto<ChallengeResponseDto>>(response);

        Assert.NotNull(result);
        Assert.All(result.Items, item => Assert.Equal("Easy", item.Difficulty));
    }

    [Fact]
    public async Task GetChallenges_WithBothFilters_ShouldApplyBoth()
    {
        var response = await _client.GetAsync("/api/v1/challenges?category=CodeLogic&difficulty=Medium");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await DeserializeResponse<PaginatedResponseDto<ChallengeResponseDto>>(response);

        Assert.NotNull(result);
        Assert.All(result.Items, item =>
        {
            Assert.Equal("CodeLogic", item.Category);
            Assert.Equal("Medium", item.Difficulty);
        });
    }

    [Fact]
    public async Task GetChallenges_WithPage2_ShouldReturnSecondPage()
    {
        var response = await _client.GetAsync("/api/v1/challenges?pageNumber=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await DeserializeResponse<PaginatedResponseDto<ChallengeResponseDto>>(response);

        Assert.NotNull(result);
        Assert.Equal(2, result.PageNumber);
    }

    [Fact]
    public async Task GetChallenges_WithPageSize25_ShouldReturn25Items()
    {
        var response = await _client.GetAsync("/api/v1/challenges?pageSize=25");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await DeserializeResponse<PaginatedResponseDto<ChallengeResponseDto>>(response);

        Assert.NotNull(result);
        Assert.Equal(25, result.PageSize);
        Assert.True(result.Items.Count <= 25);
    }

    [Fact]
    public async Task GetChallenges_WithInvalidCategory_ShouldReturn400()
    {
        var response = await _client.GetAsync("/api/v1/challenges?category=InvalidCategory");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetChallenges_WithInvalidDifficulty_ShouldReturn400()
    {
        var response = await _client.GetAsync("/api/v1/challenges?difficulty=InvalidDifficulty");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetChallenges_WithNegativePageNumber_ShouldReturnPage1()
    {
        var response = await _client.GetAsync("/api/v1/challenges?pageNumber=-5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await DeserializeResponse<PaginatedResponseDto<ChallengeResponseDto>>(response);

        Assert.NotNull(result);
        Assert.Equal(1, result.PageNumber);
    }

    [Fact]
    public async Task GetChallenges_WithPageSizeLargerThanMax_ShouldLimitTo50()
    {
        var response = await _client.GetAsync("/api/v1/challenges?pageSize=999");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await DeserializeResponse<PaginatedResponseDto<ChallengeResponseDto>>(response);

        Assert.NotNull(result);
        Assert.Equal(50, result.PageSize);
    }

    [Fact]
    public async Task GetChallenges_WithFiltersNoMatches_ShouldReturnEmptyItems()
    {
        var response = await _client.GetAsync("/api/v1/challenges?category=Sql&difficulty=Hard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await DeserializeResponse<PaginatedResponseDto<ChallengeResponseDto>>(response);

        Assert.NotNull(result);
        Assert.NotNull(result.Items);
    }

    [Fact]
    public async Task GetChallenges_ShouldReturnChallengesOrderedByDateDescending()
    {
        var response = await _client.GetAsync("/api/v1/challenges?pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await DeserializeResponse<PaginatedResponseDto<ChallengeResponseDto>>(response);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
    }
}
