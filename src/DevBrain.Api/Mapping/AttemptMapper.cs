using DevBrain.Domain.Entities;
using DevBrain.Api.DTOs;

namespace DevBrain.Api.Mapping;

public static class AttemptMapper
{
    public static AttemptResponseDto ToResponseDto(this Attempt attempt, Challenge challenge)
    {
        return new AttemptResponseDto(
            AttemptId: attempt.Id,
            ChallengeId: attempt.ChallengeId,
            UserId: attempt.UserId,
            UserAnswer: attempt.UserAnswer,
            IsCorrect: attempt.IsCorrect,
            CorrectAnswer: challenge.CorrectAnswer,
            ElapsedSeconds: attempt.ElapsedSecs,
            ChallengeTitle: challenge.Title,
            OccurredAt: attempt.OccurredAt.UtcDateTime
        );
    }
}
