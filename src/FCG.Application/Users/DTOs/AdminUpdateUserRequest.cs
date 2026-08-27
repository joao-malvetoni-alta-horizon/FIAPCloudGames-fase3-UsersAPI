using FCG.Domain.Users.Enums;

namespace FCG.Application.Users.DTOs;

public record AdminUpdateUserRequest(
    bool? IsActive,
    RoleType? Role);