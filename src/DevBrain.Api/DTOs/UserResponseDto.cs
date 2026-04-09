namespace DevBrain.Api.DTOs;

public record UserResponseDto(
    Guid UserId,
    string Email,
    string DisplayName,
    DateTime CreatedAt
);
