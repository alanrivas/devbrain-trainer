using System.Net;
using System.Text;
using System.Text.Json;
using DevBrain.Api.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DevBrain.Api.Tests;

public class PostAuthRegisterEndpointTests : IAsyncLifetime
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

    private StringContent CreateRequestContent(RegisterRequestDto dto)
    {
        var json = JsonSerializer.Serialize(dto);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private async Task<(HttpStatusCode StatusCode, dynamic? Response)> PostRegister(RegisterRequestDto request)
    {
        var response = await _client.PostAsync(
            "/api/v1/auth/register",
            CreateRequestContent(request)
        );

        var contentStr = await response.Content.ReadAsStringAsync();
        dynamic? responseData = null;

        if (!string.IsNullOrEmpty(contentStr))
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            if (response.StatusCode == HttpStatusCode.Created)
                responseData = JsonSerializer.Deserialize<UserResponseDto>(contentStr, options);
            else
                responseData = JsonSerializer.Deserialize<dynamic>(contentStr, options);
        }

        return (response.StatusCode, responseData);
    }

    // Happy Path Tests

    [Fact]
    public async Task PostRegister_ValidRequest_ShouldReturn201WithUserData()
    {
        var request = new RegisterRequestDto(
            Email: "newuser@example.com",
            Password: "ValidPass123",
            DisplayName: "John Developer"
        );

        var (statusCode, response) = await PostRegister(request);

        Assert.Equal(HttpStatusCode.Created, statusCode);
        Assert.NotNull(response);
        var userResponse = response as UserResponseDto;
        Assert.NotNull(userResponse);
        Assert.NotEqual(Guid.Empty, userResponse!.Id);
        Assert.Equal("newuser@example.com", userResponse.Email);
        Assert.Equal("John Developer", userResponse.DisplayName);
        Assert.NotEqual(default(DateTime), userResponse.CreatedAt);
    }

    [Fact]
    public async Task PostRegister_EmailNormalized_ShouldStoreLowercase()
    {
        var request = new RegisterRequestDto(
            Email: "John.Doe@EXAMPLE.COM",
            Password: "ValidPass123",
            DisplayName: "John Doe"
        );

        var (statusCode, response) = await PostRegister(request);

        Assert.Equal(HttpStatusCode.Created, statusCode);
        var userResponse = response as UserResponseDto;
        Assert.Equal("john.doe@example.com", userResponse!.Email);
    }

    [Fact]
    public async Task PostRegister_DisplayNameTrimmed_ShouldStoreWithoutLeadingTrailingSpaces()
    {
        var request = new RegisterRequestDto(
            Email: "user@example.com",
            Password: "ValidPass123",
            DisplayName: "  John Developer  "
        );

        var (statusCode, response) = await PostRegister(request);

        Assert.Equal(HttpStatusCode.Created, statusCode);
        var userResponse = response as UserResponseDto;
        Assert.Equal("John Developer", userResponse!.DisplayName);
    }

    // Email Validation Tests

    [Fact]
    public async Task PostRegister_InvalidEmailFormat_ShouldReturn400()
    {
        var request = new RegisterRequestDto(
            Email: "not-an-email",
            Password: "ValidPass123",
            DisplayName: "John Developer"
        );

        var (statusCode, response) = await PostRegister(request);

        Assert.Equal(HttpStatusCode.BadRequest, statusCode);
        Assert.NotNull(response);
    }

    [Fact]
    public async Task PostRegister_EmailMissingAtSign_ShouldReturn400()
    {
        var request = new RegisterRequestDto(
            Email: "userexample.com",
            Password: "ValidPass123",
            DisplayName: "John Developer"
        );

        var (statusCode, _) = await PostRegister(request);
        Assert.Equal(HttpStatusCode.BadRequest, statusCode);
    }

    [Fact]
    public async Task PostRegister_EmailMissingDomain_ShouldReturn400()
    {
        var request = new RegisterRequestDto(
            Email: "user@",
            Password: "ValidPass123",
            DisplayName: "John Developer"
        );

        var (statusCode, _) = await PostRegister(request);
        Assert.Equal(HttpStatusCode.BadRequest, statusCode);
    }

    // Password Validation Tests

    [Fact]
    public async Task PostRegister_PasswordTooShort_ShouldReturn400()
    {
        var request = new RegisterRequestDto(
            Email: "user@example.com",
            Password: "Pass1",
            DisplayName: "John Developer"
        );

        var (statusCode, _) = await PostRegister(request);
        Assert.Equal(HttpStatusCode.BadRequest, statusCode);
    }

    [Fact]
    public async Task PostRegister_PasswordNoUppercase_ShouldReturn400()
    {
        var request = new RegisterRequestDto(
            Email: "user@example.com",
            Password: "password123",
            DisplayName: "John Developer"
        );

        var (statusCode, _) = await PostRegister(request);
        Assert.Equal(HttpStatusCode.BadRequest, statusCode);
    }

    [Fact]
    public async Task PostRegister_PasswordNoDigit_ShouldReturn400()
    {
        var request = new RegisterRequestDto(
            Email: "user@example.com",
            Password: "PasswordNoDigit",
            DisplayName: "John Developer"
        );

        var (statusCode, _) = await PostRegister(request);
        Assert.Equal(HttpStatusCode.BadRequest, statusCode);
    }

    // DisplayName Validation Tests

    [Fact]
    public async Task PostRegister_DisplayNameTooShort_ShouldReturn400()
    {
        var request = new RegisterRequestDto(
            Email: "user@example.com",
            Password: "ValidPass123",
            DisplayName: "ab"
        );

        var (statusCode, _) = await PostRegister(request);
        Assert.Equal(HttpStatusCode.BadRequest, statusCode);
    }

    [Fact]
    public async Task PostRegister_DisplayNameTooLong_ShouldReturn400()
    {
        var request = new RegisterRequestDto(
            Email: "user@example.com",
            Password: "ValidPass123",
            DisplayName: new string('a', 51)
        );

        var (statusCode, _) = await PostRegister(request);
        Assert.Equal(HttpStatusCode.BadRequest, statusCode);
    }

    [Fact]
    public async Task PostRegister_DisplayNameInvalidCharacters_ShouldReturn400()
    {
        var request = new RegisterRequestDto(
            Email: "user@example.com",
            Password: "ValidPass123",
            DisplayName: "User@Name!"
        );

        var (statusCode, _) = await PostRegister(request);
        Assert.Equal(HttpStatusCode.BadRequest, statusCode);
    }

    // Duplicate Email Tests

    [Fact]
    public async Task PostRegister_DuplicateEmail_CaseInsensitive_ShouldReturn409()
    {
        // First registration
        var request1 = new RegisterRequestDto(
            Email: "john@example.com",
            Password: "ValidPass123",
            DisplayName: "John Developer"
        );

        var (statusCode1, _) = await PostRegister(request1);
        Assert.Equal(HttpStatusCode.Created, statusCode1);

        // Second registration with same email (different case)
        var request2 = new RegisterRequestDto(
            Email: "JOHN@EXAMPLE.COM",
            Password: "ValidPass456",
            DisplayName: "Jane Developer"
        );

        var (statusCode2, _) = await PostRegister(request2);
        Assert.Equal(HttpStatusCode.Conflict, statusCode2);
    }
}
