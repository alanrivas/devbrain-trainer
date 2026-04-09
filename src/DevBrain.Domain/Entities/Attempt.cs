using DevBrain.Domain.Exceptions;

namespace DevBrain.Domain.Entities;

public sealed class Attempt
{
    public Guid Id { get; }
    public Guid ChallengeId { get; }
    public string UserId { get; }
    public string UserAnswer { get; }
    public bool IsCorrect { get; }
    public int ElapsedSecs { get; }
    public DateTimeOffset OccurredAt { get; }

    // Navigation properties (EF Core)
    public User? User { get; set; }
    public Challenge? Challenge { get; set; }

    private Attempt(
        Guid id,
        Guid challengeId,
        string userId,
        string userAnswer,
        bool isCorrect,
        int elapsedSecs,
        DateTimeOffset occurredAt)
    {
        Id = id;
        ChallengeId = challengeId;
        UserId = userId;
        UserAnswer = userAnswer;
        IsCorrect = isCorrect;
        ElapsedSecs = elapsedSecs;
        OccurredAt = occurredAt;
    }

    public static Attempt Create(Guid challengeId, string userId, string userAnswer, int elapsedSecs, Challenge challenge)
    {
        if (challengeId == Guid.Empty)
            throw new DomainException("ChallengeId is required.");

        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId is required.");

        if (string.IsNullOrWhiteSpace(userAnswer))
            throw new DomainException("User answer is required.");

        if (elapsedSecs < 0)
            throw new DomainException("Elapsed time must be greater than or equal to 0.");

        // Note: If elapsedSecs > challenge.TimeLimitSecs, we allow it but the API can log a warning
        // This is by design - users can exceed the time limit, but we still count the attempt

        return new Attempt(
            id: Guid.NewGuid(),
            challengeId: challengeId,
            userId: userId.Trim(),
            userAnswer: userAnswer.Trim(),
            isCorrect: challenge.IsCorrectAnswer(userAnswer),
            elapsedSecs: elapsedSecs,
            occurredAt: DateTimeOffset.UtcNow
        );
    }
}
