using FCG.Domain.Users.Enums;

namespace FCG.Application.Users.DTOs;

public record AdminUpdateUserResponse(
    Guid Id,
    string Name,
    string Email,
    bool IsActive,
    RoleType Role);