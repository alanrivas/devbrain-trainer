namespace DevBrain.Api.DTOs;

public record CreateAttemptRequestDto(
    string UserAnswer,
    int ElapsedSeconds
);
