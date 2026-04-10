using DevBrain.Domain.Interfaces;
using DevBrain.Domain.Services;
using DevBrain.Infrastructure.Services;

namespace DevBrain.Api.Services;

public sealed class AttemptService : IAttemptService
{
    private readonly IChallengeRepository _challengeRepository;
    private readonly IAttemptRepository _attemptRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEloRatingService _eloRatingService;
    private readonly IStreakService _streakService;

    public AttemptService(
        IChallengeRepository challengeRepository,
        IAttemptRepository attemptRepository,
        IUserRepository userRepository,
        IEloRatingService eloRatingService,
        IStreakService streakService)
    {
        _challengeRepository = challengeRepository;
        _attemptRepository = attemptRepository;
        _userRepository = userRepository;
        _eloRatingService = eloRatingService;
        _streakService = streakService;
    }

    public async Task<AttemptResult> SubmitAsync(Guid challengeId, Guid userId, string userAnswer, int elapsedSecs)
    {
        var challenge = await _challengeRepository.GetByIdAsync(challengeId)
            ?? throw new ApplicationException($"Challenge with ID '{challengeId}' not found");

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new ApplicationException($"User with ID '{userId}' not found");

        var attempt = Domain.Entities.Attempt.Create(
            challengeId: challengeId,
            userId: userId,
            userAnswer: userAnswer,
            elapsedSecs: elapsedSecs,
            challenge: challenge
        );

        await _attemptRepository.AddAsync(attempt);

        var newEloRating = _eloRatingService.Calculate(
            userRating: user.EloRating,
            difficulty: challenge.Difficulty,
            timeLimitSecs: challenge.TimeLimitSecs,
            isCorrect: attempt.IsCorrect,
            elapsedSecs: elapsedSecs
        );

        user.UpdateEloRating(newEloRating);
        await _userRepository.UpdateAsync(user);

        var newStreak = await _streakService.RecordAttemptAsync(userId, attempt.OccurredAt);

        return new AttemptResult(
            AttemptId: attempt.Id,
            ChallengeId: attempt.ChallengeId,
            UserId: attempt.UserId,
            UserAnswer: attempt.UserAnswer,
            IsCorrect: attempt.IsCorrect,
            CorrectAnswer: challenge.CorrectAnswer,
            ElapsedSeconds: attempt.ElapsedSecs,
            ChallengeTitle: challenge.Title,
            OccurredAt: attempt.OccurredAt,
            NewEloRating: newEloRating,
            NewStreak: newStreak
        );
    }
}
