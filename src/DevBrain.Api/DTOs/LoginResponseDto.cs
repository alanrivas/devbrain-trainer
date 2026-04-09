namespace DevBrain.Api.DTOs;

public class LoginResponseDto
{
    public required string Token { get; init; }
    public required UserResponseDto User { get; init; }
}
