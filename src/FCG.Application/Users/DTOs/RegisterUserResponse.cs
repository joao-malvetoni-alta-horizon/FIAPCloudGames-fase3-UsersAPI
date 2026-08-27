using FCG.Domain.Users.Enums;

namespace FCG.Application.Users.DTOs;

public record RegisterUserResponse(
    Guid Id,
    string Name,
    string Email,
    RoleType Role);