namespace DevBrain.Api.Services;

public interface IJwtTokenService
{
    string GenerateToken(Guid userId, string email);
    (Guid UserId, string Email) ValidateToken(string token);
}
