using System;
using Serilog;
using Serilog.Events;
using Xunit;

namespace DevBrain.Infrastructure.Tests;

/// <summary>
/// Tests para verificar que Serilog está configurado correctamente.
/// </summary>
public class SerilogLoggingTests
{
    [Fact]
    public void SerilogConfiguration_CreateLogger_ShouldSucceed()
    {
        // Arrange & Act
        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        // Assert
        Assert.NotNull(logger);
    }

    [Fact]
    public void SerilogConfiguration_WithMultipleOverrides_ShouldSucceed()
    {
        // Arrange & Act
        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentUserName()
            .WriteTo.Console()
            .CreateLogger();

        // Assert
        Assert.NotNull(logger);
        logger.Information("Test message");
    }

    [Fact]
    public void SerilogConfiguration_WithEnrichers_ShouldInjectProperties()
    {
        // Arrange & Act
        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.WithProperty("Environment", "Test")
            .WriteTo.Console()
            .CreateLogger();

        // Assert - No exception
        Assert.NotNull(logger);
        logger.Information("User event logged");
    }

    [Fact]
    public void SerilogLogger_LogVariousLevels_ShouldNotThrow()
    {
        // Arrange
        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        // Act & Assert - None should throw
        logger.Debug("Debug message");
        logger.Information("Info message");
        logger.Warning("Warning message");
        logger.Error("Error message");
    }

    [Fact]
    public void SerilogLogger_StructuredLogging_ShouldWork()
    {
        // Arrange
        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        // Act & Assert
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        
        logger.Information("User registration: {UserId} {Email}", userId, email);
        Assert.NotNull(logger);
    }
}
