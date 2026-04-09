using DevBrain.Api.DTOs;
using DevBrain.Domain.Entities;

namespace DevBrain.Api.Mapping;

public static class UserMapper
{
    public static UserResponseDto ToResponseDto(this User user)
    {
        return new UserResponseDto(
            UserId: user.Id,
            Email: user.Email,
            DisplayName: user.DisplayName,
            CreatedAt: user.CreatedAt.UtcDateTime
        );
    }
}
