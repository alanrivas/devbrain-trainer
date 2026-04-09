using DevBrain.Api.DTOs;
using DevBrain.Domain.Entities;

namespace DevBrain.Api.Mapping;

public static class ChallengeMapper
{
    public static ChallengeResponseDto ToResponseDto(this Challenge challenge)
    {
        return new ChallengeResponseDto(
            Id: challenge.Id,
            Title: challenge.Title,
            Description: challenge.Description,
            Category: challenge.Category.ToString(),
            Difficulty: challenge.Difficulty.ToString(),
            TimeLimitSecs: challenge.TimeLimitSecs
        );
    }

    public static IReadOnlyList<ChallengeResponseDto> ToResponseDtos(this IEnumerable<Challenge> challenges)
    {
        return challenges
            .Select(c => c.ToResponseDto())
            .ToList()
            .AsReadOnly();
    }
}
