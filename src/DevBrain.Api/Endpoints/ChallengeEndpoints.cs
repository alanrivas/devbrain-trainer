using DevBrain.Api.DTOs;
using DevBrain.Api.Mapping;
using DevBrain.Domain.Enums;
using DevBrain.Domain.Interfaces;

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

        group.MapPost("/{id}/attempt", PostAttempt)
            .WithName("PostAttempt")
            .WithDescription("Enviar respuesta a un challenge");
    }

    private static async Task<IResult> GetChallenges(
        IChallengeRepository repository,
        string? category = null,
        string? difficulty = null,
        int pageNumber = 1,
        int pageSize = 10
    )
    {
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

        return Results.Ok(response);
    }

    private static async Task<IResult> PostAttempt(
        Guid id,
        CreateAttemptRequestDto request,
        IChallengeRepository challengeRepository,
        IAttemptRepository attemptRepository,
        HttpContext httpContext
    )
    {
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

        // Get challenge
        var challenge = await challengeRepository.GetByIdAsync(id);
        if (challenge == null)
        {
            return Results.NotFound(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                title = "Not Found",
                status = 404,
                detail = $"Challenge with ID '{id}' not found"
            });
        }

        // Extract userId from header (for testing; in production this comes from JWT)
        var userIdHeader = httpContext.Request.Headers["X-User-Id"].ToString();
        if (string.IsNullOrEmpty(userIdHeader))
        {
            return Results.Unauthorized();
        }

        // Try to parse userId as Guid
        if (!Guid.TryParse(userIdHeader, out var userId))
        {
            return Results.BadRequest(new
            {
                status = 400,
                title = "Bad Request",
                detail = "User ID in header must be a valid GUID"
            });
        }

        // Create attempt
        var attempt = Domain.Entities.Attempt.Create(
            challengeId: id,
            userId: userId,
            userAnswer: request.UserAnswer.Trim(),
            elapsedSecs: request.ElapsedSeconds,
            challenge: challenge
        );

        // Save attempt
        await attemptRepository.AddAsync(attempt);

        // Return response
        var responseDto = attempt.ToResponseDto(challenge);
        return Results.Created($"/api/v1/challenges/{id}/attempt/{attempt.Id}", responseDto);
    }
}
