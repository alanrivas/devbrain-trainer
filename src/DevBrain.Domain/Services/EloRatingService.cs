using DevBrain.Domain.Enums;

namespace DevBrain.Domain.Services;

public sealed class EloRatingService : IEloRatingService
{
    private const int K_BASE = 32;
    private const int CHALLENGE_RATING_EASY   = 800;
    private const int CHALLENGE_RATING_MEDIUM = 1200;
    private const int CHALLENGE_RATING_HARD   = 1600;
    private const int MIN_RATING = 100;

    public int Calculate(int userRating, Difficulty difficulty, int timeLimitSecs, bool isCorrect, int elapsedSecs)
    {
        var challengeRating = difficulty switch
        {
            Difficulty.Easy   => CHALLENGE_RATING_EASY,
            Difficulty.Medium => CHALLENGE_RATING_MEDIUM,
            Difficulty.Hard   => CHALLENGE_RATING_HARD,
            _                 => CHALLENGE_RATING_MEDIUM
        };

        // Step 1 — expected probability
        var expected = 1.0 / (1.0 + Math.Pow(10.0, (challengeRating - userRating) / 400.0));

        // Step 2 — score
        var score = isCorrect ? 1.0 : 0.0;

        // Step 3 — time modifier (only on correct attempts)
        double timeModifier = 1.0;
        if (isCorrect)
        {
            var timeRatio = Math.Clamp((double)elapsedSecs / timeLimitSecs, 0.0, 1.0);
            timeModifier = 1.0 + (1.0 - timeRatio) * 0.25;
        }

        // Step 4 — delta
        var delta = K_BASE * (score - expected) * timeModifier;
        var deltaRounded = (int)Math.Round(delta, MidpointRounding.AwayFromZero);

        // Step 5 — new rating with floor
        return Math.Max(MIN_RATING, userRating + deltaRounded);
    }
}
