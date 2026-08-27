namespace FCG.Application.Users.DTOs;

public record PagedUsersResponse(
    IReadOnlyList<UserSummaryResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);