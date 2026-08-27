namespace FCG.Application.Users.DTOs;

public record RegisterUserRequest(
    string Name,
    string Email,
    string Password);