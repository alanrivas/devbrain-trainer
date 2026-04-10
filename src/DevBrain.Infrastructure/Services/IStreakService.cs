namespace DevBrain.Infrastructure.Services;

public interface IStreakService
{
    /// <summary>
    /// Registra un attempt y actualiza el streak. Retorna el nuevo valor del streak.
    /// </summary>
    Task<int> RecordAttemptAsync(Guid userId, DateTimeOffset occurredAt);

    /// <summary>
    /// Retorna el streak actual del usuario. 0 si no tiene streak activo.
    /// </summary>
    Task<int> GetStreakAsync(Guid userId);
}
