namespace DevBrain.Api.DTOs;

public sealed record AttemptHistoryItemDto(
    Guid AttemptId,
    Guid ChallengeId,
    string ChallengeTitle,
    bool IsCorrect,
    int ElapsedSecs,
    DateTimeOffset OccurredAt
);