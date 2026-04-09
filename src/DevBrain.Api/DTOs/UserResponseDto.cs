namespace DevBrain.Api.DTOs;

public record UserResponseDto(
    Guid Id,
    string Email,
    string DisplayName,
    DateTime CreatedAt
);
