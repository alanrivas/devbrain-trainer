using System.Security.Claims;
using DevBrain.Api.DTOs;
using DevBrain.Domain.Interfaces;

namespace DevBrain.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/users")
            .WithName("Users");

        group.MapGet("/me/stats", GetUserStats)
            .WithName("GetUserStats")
            .WithDescription("Obtiene estadísticas del usuario autenticado")
            .RequireAuthorization();
    }

    private static async Task<IResult> GetUserStats(
        HttpContext httpContext,
        IUserRepository userRepository,
        IAttemptRepository attemptRepository
    )
    {
        // Extract userId from JWT claims (guaranteed present by RequireAuthorization)
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Results.Unauthorized();

        // Fetch user for displayName
        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
            return Results.NotFound(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                title = "Not Found",
                status = 404,
                detail = "User not found"
            });

        // Fetch attempt stats in parallel
        var attemptsTask = attemptRepository.GetByUserAsync(userId);
        var correctCountTask = attemptRepository.CountCorrectByUserAsync(userId);
        var lastAttemptTask = attemptRepository.GetLastByUserAsync(userId);

        await Task.WhenAll(attemptsTask, correctCountTask, lastAttemptTask);

        var totalAttempts = attemptsTask.Result.Count;
        var correctAttempts = correctCountTask.Result;
        var lastAttempt = lastAttemptTask.Result;

        var accuracyRate = totalAttempts > 0
            ? (float)correctAttempts / totalAttempts
            : 0.0f;

        var response = new UserStatsResponseDto(
            UserId: userId,
            DisplayName: user.DisplayName,
            TotalAttempts: totalAttempts,
            CorrectAttempts: correctAttempts,
            AccuracyRate: accuracyRate,
            CurrentStreak: 0,    // Placeholder — Fase F (Redis)
            EloRating: 1000,     // Placeholder — Fase F (ELO calc)
            LastAttemptAt: lastAttempt?.OccurredAt
        );

        return Results.Ok(response);
    }
}
