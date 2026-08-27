using FCG.Domain.Users.Enums;

namespace FCG.Application.Users.DTOs;

public record AdminCreateUserRequest(
    string Name,
    string Email,
    string Password,
    RoleType Role);