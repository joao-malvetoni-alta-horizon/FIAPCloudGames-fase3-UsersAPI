using FCG.Domain.Users.Enums;

namespace FCG.Application.Users.DTOs;

public record AdminCreateUserResponse(
    Guid Id,
    string Name,
    string Email,
    RoleType Role);