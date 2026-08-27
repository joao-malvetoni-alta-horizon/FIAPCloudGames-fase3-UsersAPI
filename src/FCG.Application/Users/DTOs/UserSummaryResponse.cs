namespace FCG.Application.Users.DTOs;

public record UserSummaryResponse(
    Guid Id,
    string Name,
    string Email,
    string Role);