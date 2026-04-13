using System;
using Serilog.Events;
using Xunit;

namespace DevBrain.Infrastructure.Tests;

/// <summary>
/// Tests para verificar que la configuración dinámica del nivel mínimo de log funciona correctamente.
/// </summary>
public class LogLevelConfigurationTests
{
    /// <summary>
    /// Helper que intenta parsear un string a LogEventLevel.
    /// Refleja la lógica que se implementará en Program.cs
    /// </summary>
    private static LogEventLevel GetLogLevelFromEnvironment(string? envValue)
    {
        var minLevelStr = envValue ?? "Information";
        
        var parseSuccess = Enum.TryParse<LogEventLevel>(
            minLevelStr,
            ignoreCase: true,
            out var parsedLevel
        );

        return parseSuccess ? parsedLevel : LogEventLevel.Information;
    }

    [Fact]
    public void GetLogLevelFromEnvironment_GivenDebug_ShouldReturnDebug()
    {
        // Arrange
        var envValue = "Debug";

        // Act
        var logLevel = GetLogLevelFromEnvironment(envValue);

        // Assert
        Assert.Equal(LogEventLevel.Debug, logLevel);
    }

    [Fact]
    public void GetLogLevelFromEnvironment_GivenInformation_ShouldReturnInformation()
    {
        // Arrange
        var envValue = "Information";

        // Act
        var logLevel = GetLogLevelFromEnvironment(envValue);

        // Assert
        Assert.Equal(LogEventLevel.Information, logLevel);
    }

    [Fact]
    public void GetLogLevelFromEnvironment_GivenWarning_ShouldReturnWarning()
    {
        // Arrange
        var envValue = "Warning";

        // Act
        var logLevel = GetLogLevelFromEnvironment(envValue);

        // Assert
        Assert.Equal(LogEventLevel.Warning, logLevel);
    }

    [Fact]
    public void GetLogLevelFromEnvironment_GivenError_ShouldReturnError()
    {
        // Arrange
        var envValue = "Error";

        // Act
        var logLevel = GetLogLevelFromEnvironment(envValue);

        // Assert
        Assert.Equal(LogEventLevel.Error, logLevel);
    }

    [Fact]
    public void GetLogLevelFromEnvironment_GivenFatal_ShouldReturnFatal()
    {
        // Arrange
        var envValue = "Fatal";

        // Act
        var logLevel = GetLogLevelFromEnvironment(envValue);

        // Assert
        Assert.Equal(LogEventLevel.Fatal, logLevel);
    }

    [Fact]
    public void GetLogLevelFromEnvironment_GivenVerbose_ShouldReturnVerbose()
    {
        // Arrange
        var envValue = "Verbose";

        // Act
        var logLevel = GetLogLevelFromEnvironment(envValue);

        // Assert
        Assert.Equal(LogEventLevel.Verbose, logLevel);
    }

    [Fact]
    public void GetLogLevelFromEnvironment_GivenLowercaseDebug_ShouldReturnDebug()
    {
        // Arrange
        var envValue = "debug";

        // Act
        var logLevel = GetLogLevelFromEnvironment(envValue);

        // Assert
        Assert.Equal(LogEventLevel.Debug, logLevel);
    }

    [Fact]
    public void GetLogLevelFromEnvironment_GivenMixedCaseDebug_ShouldReturnDebug()
    {
        // Arrange
        var envValue = "DeBuG";

        // Act
        var logLevel = GetLogLevelFromEnvironment(envValue);

        // Assert
        Assert.Equal(LogEventLevel.Debug, logLevel);
    }

    [Fact]
    public void GetLogLevelFromEnvironment_GivenUppercaseInformation_ShouldReturnInformation()
    {
        // Arrange
        var envValue = "INFORMATION";

        // Act
        var logLevel = GetLogLevelFromEnvironment(envValue);

        // Assert
        Assert.Equal(LogEventLevel.Information, logLevel);
    }

    [Fact]
    public void GetLogLevelFromEnvironment_GivenInvalidValue_ShouldDefaultToInformation()
    {
        // Arrange
        var envValue = "InvalidLevel";

        // Act
        var logLevel = GetLogLevelFromEnvironment(envValue);

        // Assert
        Assert.Equal(LogEventLevel.Information, logLevel);
    }

    [Fact]
    public void GetLogLevelFromEnvironment_GivenEmptyString_ShouldDefaultToInformation()
    {
        // Arrange
        var envValue = "";

        // Act
        var logLevel = GetLogLevelFromEnvironment(envValue);

        // Assert
        Assert.Equal(LogEventLevel.Information, logLevel);
    }

    [Fact]
    public void GetLogLevelFromEnvironment_GivenNull_ShouldDefaultToInformation()
    {
        // Arrange
        string? envValue = null;

        // Act
        var logLevel = GetLogLevelFromEnvironment(envValue);

        // Assert
        Assert.Equal(LogEventLevel.Information, logLevel);
    }

    [Fact]
    public void GetLogLevelFromEnvironment_GivenWhitespace_ShouldDefaultToInformation()
    {
        // Arrange
        var envValue = "   ";

        // Act
        var logLevel = GetLogLevelFromEnvironment(envValue);

        // Assert
        Assert.Equal(LogEventLevel.Information, logLevel);
    }
}
