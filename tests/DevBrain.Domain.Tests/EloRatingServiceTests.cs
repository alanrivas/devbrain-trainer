using DevBrain.Domain.Enums;
using DevBrain.Domain.Services;

namespace DevBrain.Domain.Tests;

public class EloRatingServiceTests
{
    private readonly IEloRatingService _sut = new EloRatingService();

    // ─── Rating sube cuando es correcto ────────────────────────────────────────

    [Fact]
    public void Calculate_Correct_Easy_ShouldIncreaseRating()
    {
        var newRating = _sut.Calculate(userRating: 1000, Difficulty.Easy, timeLimitSecs: 60, isCorrect: true, elapsedSecs: 30);

        Assert.True(newRating > 1000);
    }

    [Fact]
    public void Calculate_Correct_Medium_ShouldIncreaseMoreThanEasy()
    {
        var easyRating  = _sut.Calculate(userRating: 1000, Difficulty.Easy,   timeLimitSecs: 60, isCorrect: true, elapsedSecs: 30);
        var mediumRating = _sut.Calculate(userRating: 1000, Difficulty.Medium, timeLimitSecs: 60, isCorrect: true, elapsedSecs: 30);

        Assert.True(mediumRating > easyRating);
    }

    [Fact]
    public void Calculate_Correct_Hard_ShouldIncreaseMoreThanMedium()
    {
        var mediumRating = _sut.Calculate(userRating: 1000, Difficulty.Medium, timeLimitSecs: 60, isCorrect: true, elapsedSecs: 30);
        var hardRating   = _sut.Calculate(userRating: 1000, Difficulty.Hard,   timeLimitSecs: 60, isCorrect: true, elapsedSecs: 30);

        Assert.True(hardRating > mediumRating);
    }

    // ─── Rating baja cuando es incorrecto ──────────────────────────────────────

    [Fact]
    public void Calculate_Incorrect_Easy_ShouldDecreaseRating()
    {
        var newRating = _sut.Calculate(userRating: 1000, Difficulty.Easy, timeLimitSecs: 60, isCorrect: false, elapsedSecs: 30);

        Assert.True(newRating < 1000);
    }

    [Fact]
    public void Calculate_Incorrect_Hard_ShouldDecreaseLessthanEasy()
    {
        // Hard challenge: menor expected → menos puntos perdidos si falla
        var easyDelta = 1000 - _sut.Calculate(userRating: 1000, Difficulty.Easy, timeLimitSecs: 60, isCorrect: false, elapsedSecs: 30);
        var hardDelta = 1000 - _sut.Calculate(userRating: 1000, Difficulty.Hard, timeLimitSecs: 60, isCorrect: false, elapsedSecs: 30);

        Assert.True(hardDelta < easyDelta);
    }

    // ─── Time modifier ─────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_Correct_FasterResponse_ShouldGiveMorePoints()
    {
        var fastRating = _sut.Calculate(userRating: 1000, Difficulty.Easy, timeLimitSecs: 60, isCorrect: true, elapsedSecs: 0);
        var slowRating = _sut.Calculate(userRating: 1000, Difficulty.Easy, timeLimitSecs: 60, isCorrect: true, elapsedSecs: 60);

        Assert.True(fastRating > slowRating);
    }

    [Fact]
    public void Calculate_Incorrect_TimeModifierDoesNotApply()
    {
        // Incorrecto: el tiempo no debería cambiar el resultado
        var fastRating = _sut.Calculate(userRating: 1000, Difficulty.Easy, timeLimitSecs: 60, isCorrect: false, elapsedSecs: 0);
        var slowRating = _sut.Calculate(userRating: 1000, Difficulty.Easy, timeLimitSecs: 60, isCorrect: false, elapsedSecs: 60);

        Assert.Equal(fastRating, slowRating);
    }

    // ─── Floor de rating ───────────────────────────────────────────────────────

    [Fact]
    public void Calculate_RatingNeverDropsBelowFloor()
    {
        var newRating = _sut.Calculate(userRating: 100, Difficulty.Easy, timeLimitSecs: 60, isCorrect: false, elapsedSecs: 30);

        Assert.True(newRating >= 100);
    }

    [Fact]
    public void Calculate_LowRating_Incorrect_StaysAt100()
    {
        var newRating = _sut.Calculate(userRating: 100, Difficulty.Easy, timeLimitSecs: 60, isCorrect: false, elapsedSecs: 30);

        Assert.Equal(100, newRating);
    }

    // ─── Valores específicos (anclan la fórmula exacta) ────────────────────────

    [Fact]
    public void Calculate_Correct_Easy_Instant_ExactDelta()
    {
        // user=1000, Easy(800), correcto, elapsed=0/60
        // expected ≈ 0.7597, timeModifier = 1.25
        // delta = round(32 * (1 - 0.7597) * 1.25) = round(9.61) = 10
        var newRating = _sut.Calculate(userRating: 1000, Difficulty.Easy, timeLimitSecs: 60, isCorrect: true, elapsedSecs: 0);

        Assert.Equal(1010, newRating);
    }

    [Fact]
    public void Calculate_Incorrect_Hard_ExactDelta()
    {
        // user=1000, Hard(1600), incorrecto, elapsed=30/60
        // expected ≈ 0.0306, timeModifier = 1.0
        // delta = round(32 * (0 - 0.0306) * 1.0) = round(-0.979) = -1
        var newRating = _sut.Calculate(userRating: 1000, Difficulty.Hard, timeLimitSecs: 60, isCorrect: false, elapsedSecs: 30);

        Assert.Equal(999, newRating);
    }

    [Fact]
    public void Calculate_Correct_AtTimeLimit_NoTimeBonus()
    {
        // elapsed == timeLimitSecs → timeRatio = 1.0 → timeModifier = 1.0
        var atLimit  = _sut.Calculate(userRating: 1000, Difficulty.Medium, timeLimitSecs: 60, isCorrect: true, elapsedSecs: 60);
        // elapsed > timeLimitSecs clamped to 1.0 → same modifier
        var overLimit = _sut.Calculate(userRating: 1000, Difficulty.Medium, timeLimitSecs: 60, isCorrect: true, elapsedSecs: 90);

        Assert.Equal(atLimit, overLimit);
    }
}
