using DevBrain.Domain.Enums;

namespace DevBrain.Domain.Services;

public interface IEloRatingService
{
    /// <summary>
    /// Calcula el nuevo rating ELO del usuario tras un attempt.
    /// </summary>
    int Calculate(int userRating, Difficulty difficulty, int timeLimitSecs, bool isCorrect, int elapsedSecs);
}
