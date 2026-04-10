using DevBrain.Domain.Entities;
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
    private readonly IBadgeRepository _badgeRepository;
    private readonly IBadgeAwardService _badgeAwardService;

    public AttemptService(
        IChallengeRepository challengeRepository,
        IAttemptRepository attemptRepository,
        IUserRepository userRepository,
        IEloRatingService eloRatingService,
        IStreakService streakService,
        IBadgeRepository badgeRepository,
        IBadgeAwardService badgeAwardService)
    {
        _challengeRepository = challengeRepository;
        _attemptRepository = attemptRepository;
        _userRepository = userRepository;
        _eloRatingService = eloRatingService;
        _streakService = streakService;
        _badgeRepository = badgeRepository;
        _badgeAwardService = badgeAwardService;
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

        // Badge evaluation — get all attempts (desc by OccurredAt) to compute consecutiveCorrect
        var allAttempts = await _attemptRepository.GetByUserAsync(userId);
        var totalAttempts = allAttempts.Count;

        int consecutiveCorrect;
        if (!attempt.IsCorrect)
        {
            consecutiveCorrect = 0;
        }
        else
        {
            var count = 0;
            foreach (var a in allAttempts)   // ordered desc by OccurredAt
            {
                if (a.IsCorrect) count++;
                else break;
            }
            consecutiveCorrect = count;
        }

        var alreadyEarned = (await _badgeRepository.GetByUserAsync(userId))
            .Select(b => b.Type)
            .ToList();

        var badgeContext = new BadgeAwardContext(
            IsCorrect: attempt.IsCorrect,
            Difficulty: challenge.Difficulty,
            TotalAttempts: totalAttempts,
            ConsecutiveCorrect: consecutiveCorrect,
            CurrentStreak: newStreak,
            NewEloRating: newEloRating
        );

        var newBadgeTypes = _badgeAwardService.EvaluateNewBadges(badgeContext, alreadyEarned);

        foreach (var badgeType in newBadgeTypes)
        {
            await _badgeRepository.AddAsync(UserBadge.Create(userId, badgeType));
        }

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
            NewStreak: newStreak,
            NewBadges: newBadgeTypes.Select(b => b.ToString()).ToList()
        );
    }
}
