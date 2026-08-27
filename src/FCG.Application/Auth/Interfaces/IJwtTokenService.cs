using FCG.Domain.Users.Entities;

namespace FCG.Application.Auth.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}