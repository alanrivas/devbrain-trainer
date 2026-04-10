using DevBrain.Domain.Enums;

namespace DevBrain.Domain.Services;

public record BadgeAwardContext(
    bool IsCorrect,
    Difficulty Difficulty,
    int TotalAttempts,
    int ConsecutiveCorrect,
    int CurrentStreak,
    int NewEloRating);
