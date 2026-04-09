namespace DevBrain.Api.DTOs;

public sealed record ChallengeResponseDto(
    Guid Id,
    string Title,
    string Description,
    string Category,
    string Difficulty,
    int TimeLimitSecs
);
