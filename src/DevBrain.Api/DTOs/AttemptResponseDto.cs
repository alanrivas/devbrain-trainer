namespace DevBrain.Api.DTOs;

public record AttemptResponseDto(
    Guid AttemptId,
    Guid ChallengeId,
    Guid UserId,
    string UserAnswer,
    bool IsCorrect,
    string CorrectAnswer,
    int ElapsedSeconds,
    string ChallengeTitle,
    DateTime OccurredAt,
    int NewEloRating,
    int NewStreak,
    string[] NewBadges
);
