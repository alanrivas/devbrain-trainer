using DevBrain.Domain.Exceptions;

namespace DevBrain.Domain.Entities;

public sealed class Attempt
{
    public Guid Id { get; }
    public Guid ChallengeId { get; }
    public string UserAnswer { get; }
    public bool IsCorrect { get; }
    public int ElapsedSecs { get; }
    public DateTimeOffset OccurredAt { get; }

    private Attempt(
        Guid id,
        Guid challengeId,
        string userAnswer,
        bool isCorrect,
        int elapsedSecs,
        DateTimeOffset occurredAt)
    {
        Id = id;
        ChallengeId = challengeId;
        UserAnswer = userAnswer;
        IsCorrect = isCorrect;
        ElapsedSecs = elapsedSecs;
        OccurredAt = occurredAt;
    }

    public static Attempt Create(Guid challengeId, string userAnswer, int elapsedSecs, Challenge challenge)
    {
        if (challengeId == Guid.Empty)
            throw new DomainException("ChallengeId is required.");

        if (string.IsNullOrWhiteSpace(userAnswer))
            throw new DomainException("User answer is required.");

        if (elapsedSecs <= 0)
            throw new DomainException("Elapsed time must be greater than 0.");

        if (elapsedSecs > challenge.TimeLimitSecs)
            throw new DomainException($"Elapsed time cannot exceed the challenge time limit of {challenge.TimeLimitSecs} seconds.");

        return new Attempt(
            id: Guid.NewGuid(),
            challengeId: challengeId,
            userAnswer: userAnswer.Trim(),
            isCorrect: challenge.IsCorrectAnswer(userAnswer),
            elapsedSecs: elapsedSecs,
            occurredAt: DateTimeOffset.UtcNow
        );
    }
}
