namespace FCG.Application.Users.DTOs;

public record UserDetailResponse(
    Guid Id,
    string Name,
    string Email,
    string Role,
    bool IsActive);