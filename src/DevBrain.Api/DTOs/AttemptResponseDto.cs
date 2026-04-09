namespace DevBrain.Api.DTOs;

public record AttemptResponseDto(
    Guid AttemptId,
    Guid ChallengeId,
    string UserId,
    string UserAnswer,
    bool IsCorrect,
    string CorrectAnswer,
    int ElapsedSeconds,
    string ChallengeTitle,
    DateTime OccurredAt
);
