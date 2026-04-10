using System.Security.Claims;
using DevBrain.Api.DTOs;
using DevBrain.Api.Mapping;
using DevBrain.Api.Services;
using DevBrain.Domain.Enums;
using DevBrain.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace DevBrain.Api.Endpoints;

public static class ChallengeEndpoints
{
    public static void MapChallengeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/challenges")
            .WithName("Challenges");

        group.MapGet("/", GetChallenges)
            .WithName("GetChallenges")
            .WithDescription("Obtiene lista paginada de challenges con filtros opcionales");

        group.MapGet("/{id}", GetChallenge)
            .WithName("GetChallenge")
            .WithDescription("Obtiene detalles de un challenge específico");

        group.MapPost("/{id}/attempt", PostAttempt)
            .WithName("PostAttempt")
            .WithDescription("Enviar respuesta a un challenge")
            .RequireAuthorization();
    }

    private static async Task<IResult> GetChallenges(
        IChallengeRepository repository,
        ILogger logger,
        string? category = null,
        string? difficulty = null,
        int pageNumber = 1,
        int pageSize = 10
    )
    {
        logger.LogInformation("GetChallenges called with filters: Category={Category}, Difficulty={Difficulty}, PageNumber={PageNumber}, PageSize={PageSize}",
            category ?? "null", difficulty ?? "null", pageNumber, pageSize);
        
        // Normalize pagination
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 50);

        // Parse and validate category filter
        ChallengeCategory? parsedCategory = null;
        if (!string.IsNullOrWhiteSpace(category))
        {
            if (!Enum.TryParse<ChallengeCategory>(category, ignoreCase: true, out var parsedCat))
            {
                return Results.BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "One or more validation errors occurred.",
                    status = 400,
                    errors = new
                    {
                        category = new[] { $"Invalid category value. Valid values are: {string.Join(", ", Enum.GetNames(typeof(ChallengeCategory)))}" }
                    }
                });
            }
            parsedCategory = parsedCat;
        }

        // Parse and validate difficulty filter
        Difficulty? parsedDifficulty = null;
        if (!string.IsNullOrWhiteSpace(difficulty))
        {
            if (!Enum.TryParse<Difficulty>(difficulty, ignoreCase: true, out var parsedDiff))
            {
                return Results.BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "One or more validation errors occurred.",
                    status = 400,
                    errors = new
                    {
                        difficulty = new[] { $"Invalid difficulty value. Valid values are: {string.Join(", ", Enum.GetNames(typeof(Difficulty)))}" }
                    }
                });
            }
            parsedDifficulty = parsedDiff;
        }

        // Get all challenges (filtered by category/difficulty)
        var allChallenges = await repository.GetAllAsync(parsedCategory, parsedDifficulty);

        // Apply pagination
        var totalCount = allChallenges.Count;
        var totalPages = (totalCount + pageSize - 1) / pageSize;

        var paginatedItems = allChallenges
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToResponseDtos();

        var response = new PaginatedResponseDto<ChallengeResponseDto>(
            PageNumber: pageNumber,
            PageSize: pageSize,
            TotalCount: totalCount,
            TotalPages: totalPages,
            Items: paginatedItems
        );

        logger.LogInformation("GetChallenges completed: {Count} challenges returned, TotalPages: {TotalPages}",
            paginatedItems.Count(), totalPages);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetChallenge(
        Guid id,
        IChallengeRepository repository,
        ILogger logger
    )
    {
        logger.LogInformation("GetChallenge called with ID: {ChallengeId}", id);
        
        // Validate GUID format (though ASP.NET handles this automatically)
        if (id == Guid.Empty)
            return Results.BadRequest(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Bad Request",
                status = 400,
                detail = "Invalid challenge ID format. Must be a valid GUID."
            });

        // Fetch challenge by ID
        var challenge = await repository.GetByIdAsync(id);

        if (challenge == null)
        {
            logger.LogWarning("Challenge not found: ID={ChallengeId}", id);
            return Results.NotFound(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                title = "Not Found",
                status = 404,
                detail = "Challenge not found"
            });
        }

        // Map to response DTO (hides correctAnswer and createdAt)
        var response = challenge.ToResponseDto();
        logger.LogInformation("Challenge found and returned: ID={ChallengeId}, Title={Title}", id, challenge.Title);
        return Results.Ok(response);
    }

    private static async Task<IResult> PostAttempt(
        Guid id,
        CreateAttemptRequestDto request,
        IAttemptService attemptService,
        HttpContext httpContext,
        ILogger logger
    )
    {
        // Extract userId from JWT claims early for logging context
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Results.Unauthorized();
        
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("ChallengeId", id))
        {
            logger.LogInformation("PostAttempt called: ChallengeId={ChallengeId}, UserId={UserId}, ElapsedSeconds={ElapsedSeconds}",
                id, userId, request.ElapsedSeconds);
            
            // Validate userAnswer
            if (string.IsNullOrWhiteSpace(request.UserAnswer))
        {
            return Results.BadRequest(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "One or more validation errors occurred.",
                status = 400,
                errors = new
                {
                    userAnswer = new[] { "Field is required and cannot be empty" }
                }
            });
        }

        // Validate elapsedSeconds
        if (request.ElapsedSeconds < 0 || request.ElapsedSeconds > 3600)
        {
            return Results.BadRequest(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "One or more validation errors occurred.",
                status = 400,
                errors = new
                {
                    elapsedSeconds = new[] { "Must be between 0 and 3600 seconds" }
                }
            });
        }

            // Delegate to AttemptService
            AttemptResult result;
            try
            {
                result = await attemptService.SubmitAsync(id, userId, request.UserAnswer.Trim(), request.ElapsedSeconds);
            }
            catch (ApplicationException ex) when (ex.Message.Contains("not found"))
            {
                logger.LogWarning("Challenge not found for attempt submission: ID={ChallengeId}", id);
                return Results.NotFound(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    title = "Not Found",
                    status = 404,
                    detail = ex.Message
                });
            }

            logger.LogInformation("Attempt recorded: IsCorrect={IsCorrect}, NewELO={NewELO}, NewStreak={NewStreak}, BadgeCount={BadgeCount}",
                result.IsCorrect, result.NewEloRating, result.NewStreak, result.NewBadges.Count());

            var responseDto = new AttemptResponseDto(
                AttemptId: result.AttemptId,
                ChallengeId: result.ChallengeId,
                UserId: result.UserId,
                UserAnswer: result.UserAnswer,
                IsCorrect: result.IsCorrect,
                CorrectAnswer: result.CorrectAnswer,
                ElapsedSeconds: result.ElapsedSeconds,
                ChallengeTitle: result.ChallengeTitle,
                OccurredAt: result.OccurredAt.UtcDateTime,
                NewEloRating: result.NewEloRating,
                NewStreak: result.NewStreak,
                NewBadges: result.NewBadges.ToArray()
            );

            return Results.Created($"/api/v1/challenges/{id}/attempt/{result.AttemptId}", responseDto);
        }
    }
}
