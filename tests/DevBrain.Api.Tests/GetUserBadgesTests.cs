using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DevBrain.Api.DTOs;
using DevBrain.Api.Services;
using DevBrain.Domain.Entities;
using DevBrain.Domain.Enums;

namespace DevBrain.Api.Tests;

public class GetUserBadgesTests : IAsyncLifetime
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
        var passwordHash = passwordHashService.HashPassword("BadgesTest123!");
        var user = User.CreateFromRegistration(
            email: "badges-tester@example.com",
            passwordHash: passwordHash,
            displayName: "Badges Tester"
        );
        db.Users.Add(user);
        await db.SaveChangesAsync();
        _userId = user.Id;

        var loginRequest = new { email = "badges-tester@example.com", password = "BadgesTest123!" };
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

    private record UserBadgeResponse(string Type, DateTimeOffset EarnedAt);

    [Fact]
    public async Task GetUserBadges_WithoutToken_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/v1/users/me/badges");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserBadges_AuthenticatedWithNoBadges_ShouldReturn200WithEmptyArray()
    {
        SetAuthHeader();

        var response = await _client.GetAsync("/api/v1/users/me/badges");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await Deserialize<List<UserBadgeResponse>>(response);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUserBadges_AuthenticatedWithTwoBadgesInDB_ShouldReturn200WithArray()
    {
        // Create a fresh user to avoid interference with other tests
        var db = await _factory.GetDbContextAsync();
        var passwordHashService = new PasswordHashService();
        var passwordHash = passwordHashService.HashPassword("TwoBadges123!");
        var freshUser = User.CreateFromRegistration("badges-two@example.com", passwordHash, "Two Badges User");
        var badge1 = UserBadge.Create(freshUser.Id, BadgeType.FirstBlood);
        var badge2 = UserBadge.Create(freshUser.Id, BadgeType.Brave);
        db.Users.Add(freshUser);
        db.UserBadges.AddRange(badge1, badge2);
        await db.SaveChangesAsync();

        var loginContent = new StringContent(
            JsonSerializer.Serialize(new { email = "badges-two@example.com", password = "TwoBadges123!" }),
            Encoding.UTF8, "application/json"
        );
        var loginResponse = await _client.PostAsync("/api/v1/auth/login", loginContent);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        var freshClient = _factory.CreateClient();
        freshClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginBody!.Token);

        var response = await freshClient.GetAsync("/api/v1/users/me/badges");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await Deserialize<List<UserBadgeResponse>>(response);
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, b => b.Type == "FirstBlood");
        Assert.Contains(result, b => b.Type == "Brave");
    }

    [Fact]
    public async Task GetUserBadges_EarnedAt_ShouldBeUtcOffset()
    {
        // Create a fresh user to avoid interference with other tests
        var db = await _factory.GetDbContextAsync();
        var passwordHashService = new PasswordHashService();
        var passwordHash = passwordHashService.HashPassword("EarnedAt123!");
        var freshUser = User.CreateFromRegistration("badges-earnedat@example.com", passwordHash, "EarnedAt User");
        var badge = UserBadge.Create(freshUser.Id, BadgeType.OnFire);
        db.Users.Add(freshUser);
        db.UserBadges.Add(badge);
        await db.SaveChangesAsync();

        var loginContent = new StringContent(
            JsonSerializer.Serialize(new { email = "badges-earnedat@example.com", password = "EarnedAt123!" }),
            Encoding.UTF8, "application/json"
        );
        var loginResponse = await _client.PostAsync("/api/v1/auth/login", loginContent);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        var freshClient = _factory.CreateClient();
        freshClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginBody!.Token);

        var response = await freshClient.GetAsync("/api/v1/users/me/badges");

        var result = await Deserialize<List<UserBadgeResponse>>(response);
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(TimeSpan.Zero, result[0].EarnedAt.Offset);
    }
}
