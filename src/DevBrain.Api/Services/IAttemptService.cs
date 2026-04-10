namespace DevBrain.Api.Services;

public interface IAttemptService
{
    Task<AttemptResult> SubmitAsync(Guid challengeId, Guid userId, string userAnswer, int elapsedSecs);
}

public sealed record AttemptResult(
    Guid AttemptId,
    Guid ChallengeId,
    Guid UserId,
    string UserAnswer,
    bool IsCorrect,
    string CorrectAnswer,
    int ElapsedSeconds,
    string ChallengeTitle,
    DateTimeOffset OccurredAt,
    int NewEloRating,
    int NewStreak
);
