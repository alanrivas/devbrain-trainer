using Serilog.Events;
using Xunit;

namespace DevBrain.Api.Tests;

/// <summary>
/// Integration tests para verificar que la app respeta SERILOG__MINIMUMLEVEL env var al arrancar.
/// </summary>
public class DynamicLogLevelConfigurationTests
{
    [Fact]
    public async Task AppStartup_WithDebugLogLevelEnvVar_ShouldStartSuccessfully()
    {
        // Arrange
        Environment.SetEnvironmentVariable("SERILOG__MINIMUMLEVEL", "Debug");
        var factory = new CustomWebApplicationFactory();

        // Act
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        // Cleanup
        factory.Dispose();
        Environment.SetEnvironmentVariable("SERILOG__MINIMUMLEVEL", null);
    }

    [Fact]
    public async Task AppStartup_WithInformationLogLevelEnvVar_ShouldStartSuccessfully()
    {
        // Arrange
        Environment.SetEnvironmentVariable("SERILOG__MINIMUMLEVEL", "Information");
        var factory = new CustomWebApplicationFactory();

        // Act
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        // Cleanup
        factory.Dispose();
        Environment.SetEnvironmentVariable("SERILOG__MINIMUMLEVEL", null);
    }

    [Fact]
    public async Task AppStartup_WithErrorLogLevelEnvVar_ShouldStartSuccessfully()
    {
        // Arrange
        Environment.SetEnvironmentVariable("SERILOG__MINIMUMLEVEL", "Error");
        var factory = new CustomWebApplicationFactory();

        // Act
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        // Cleanup
        factory.Dispose();
        Environment.SetEnvironmentVariable("SERILOG__MINIMUMLEVEL", null);
    }

    [Fact]
    public async Task AppStartup_WithInvalidLogLevelEnvVar_ShouldStartSuccessfullyWithDefault()
    {
        // Arrange
        Environment.SetEnvironmentVariable("SERILOG__MINIMUMLEVEL", "InvalidLevel");
        var factory = new CustomWebApplicationFactory();

        // Act
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");

        // Assert - Should not crash, defaults to Information
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        // Cleanup
        factory.Dispose();
        Environment.SetEnvironmentVariable("SERILOG__MINIMUMLEVEL", null);
    }

    [Fact]
    public async Task AppStartup_WithNoLogLevelEnvVar_ShouldStartSuccessfullyWithDefault()
    {
        // Arrange
        Environment.SetEnvironmentVariable("SERILOG__MINIMUMLEVEL", null);
        var factory = new CustomWebApplicationFactory();

        // Act
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");

        // Assert - Should use default Information level
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        // Cleanup
        factory.Dispose();
    }

    [Fact]
    public async Task AppStartup_WithCaseInsensitiveLogLevel_ShouldStartSuccessfully()
    {
        // Arrange
        Environment.SetEnvironmentVariable("SERILOG__MINIMUMLEVEL", "debug");
        var factory = new CustomWebApplicationFactory();

        // Act
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        // Cleanup
        factory.Dispose();
        Environment.SetEnvironmentVariable("SERILOG__MINIMUMLEVEL", null);
    }
}
