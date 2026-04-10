namespace DevBrain.Api.DTOs;

public sealed record UserStatsResponseDto(
    Guid UserId,
    string DisplayName,
    int TotalAttempts,
    int CorrectAttempts,
    float AccuracyRate,
    int CurrentStreak,
    int EloRating,
    DateTimeOffset? LastAttemptAt
);
