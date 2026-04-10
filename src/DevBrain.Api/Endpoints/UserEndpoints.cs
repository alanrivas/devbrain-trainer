using System.Security.Claims;
using DevBrain.Api.DTOs;
using DevBrain.Domain.Interfaces;
using DevBrain.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Serilog.Context;

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

        group.MapGet("/me/badges", GetUserBadges)
            .WithName("GetUserBadges")
            .WithDescription("Obtiene los badges del usuario autenticado")
            .RequireAuthorization();
    }

    private static async Task<IResult> GetUserStats(
        HttpContext httpContext,
        IUserRepository userRepository,
        IAttemptRepository attemptRepository,
        IStreakService streakService,
        ILogger logger
    )
    {
        // Extract userId from JWT claims (guaranteed present by RequireAuthorization)
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Results.Unauthorized();
        
        using (LogContext.PushProperty("UserId", userId))
        {
            logger.LogInformation("GetUserStats called");

            // Fetch user for displayName
            var user = await userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                logger.LogWarning("User not found: ID={UserId}", userId);
                return Results.NotFound(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    title = "Not Found",
                    status = 404,
                    detail = "User not found"
                });
            }

            // Fetch attempt stats sequentially to avoid DbContext concurrency issues
            var totalAttempts = (await attemptRepository.GetByUserAsync(userId)).Count;
            var correctAttempts = await attemptRepository.CountCorrectByUserAsync(userId);
            var lastAttempt = await attemptRepository.GetLastByUserAsync(userId);

            var accuracyRate = totalAttempts > 0
                ? (float)correctAttempts / totalAttempts * 100f
                : 0.0f;

            var currentStreak = await streakService.GetStreakAsync(userId);
            
            logger.LogInformation("GetUserStats completed: TotalAttempts={TotalAttempts}, Accuracy={Accuracy}%, Streak={Streak}, ELO={ELO}",
                totalAttempts, accuracyRate, currentStreak, user.EloRating);

            var response = new UserStatsResponseDto(
                UserId: userId,
                DisplayName: user.DisplayName,
                TotalAttempts: totalAttempts,
                CorrectAttempts: correctAttempts,
                AccuracyRate: accuracyRate,
                CurrentStreak: currentStreak,
                EloRating: user.EloRating,
                LastAttemptAt: lastAttempt?.OccurredAt
            );

            return Results.Ok(response);
        }
    }

    private static async Task<IResult> GetUserBadges(
        HttpContext httpContext,
        IUserRepository userRepository,
        IBadgeRepository badgeRepository,
        ILogger logger
    )
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Results.Unauthorized();

        using (LogContext.PushProperty("UserId", userId))
        {
            logger.LogInformation("GetUserBadges called");

            var user = await userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                logger.LogWarning("User not found: ID={UserId}", userId);
                return Results.NotFound(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    title = "Not Found",
                    status = 404,
                    detail = "User not found"
                });
            }

            var badges = await badgeRepository.GetByUserAsync(userId);
            logger.LogInformation("GetUserBadges completed: BadgeCount={BadgeCount}", badges.Count());
            
            return Results.Ok(badges.Select(b => new UserBadgeResponseDto(b.Type.ToString(), b.EarnedAt)));
        }
    }
}
