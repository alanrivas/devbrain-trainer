using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DevBrain.Api.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private const int DefaultExpirationHours = 24;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(Guid userId, string email)
    {
        var secret = _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT:Secret not configured");
        var expirationHours = _configuration.GetValue("Jwt:ExpirationHours", DefaultExpirationHours);

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: null,
            audience: null,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expirationHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (Guid UserId, string Email) ValidateToken(string token)
    {
        var secret = _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT:Secret not configured");
        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret));

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? throw new InvalidOperationException("Token missing 'sub' claim");
            var emailClaim = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                ?? throw new InvalidOperationException("Token missing 'email' claim");

            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new InvalidOperationException("Invalid userId format in token");

            return (userId, emailClaim);
        }
        catch (SecurityTokenException ex)
        {
            throw new InvalidOperationException("Token validation failed", ex);
        }
    }
}
