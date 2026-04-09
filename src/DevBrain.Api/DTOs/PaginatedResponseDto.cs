namespace DevBrain.Api.DTOs;

public sealed record PaginatedResponseDto<T>(
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyList<T> Items
);
