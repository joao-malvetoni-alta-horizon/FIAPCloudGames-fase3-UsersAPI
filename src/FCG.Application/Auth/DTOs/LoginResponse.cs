namespace FCG.Application.Auth.DTOs;

public record LoginResponse(string AccessToken, string TokenType, int ExpiresIn);