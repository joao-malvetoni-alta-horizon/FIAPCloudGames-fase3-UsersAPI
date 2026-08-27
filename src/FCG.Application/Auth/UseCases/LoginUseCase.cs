using FCG.Application.Auth.DTOs;
using FCG.Application.Auth.Interfaces;
using FCG.Domain.Users.Exceptions;
using FCG.Domain.Users.Interfaces;

namespace FCG.Application.Auth.UseCases;

public class LoginUseCase(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService) : ILoginUseCase
{
    private static readonly int TokenExpirationInSeconds = (int)TimeSpan.FromHours(4).TotalSeconds;

    public async Task<LoginResponse> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !user.IsActive || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        var token = jwtTokenService.GenerateToken(user);
        return new LoginResponse(token, "Bearer", TokenExpirationInSeconds);
    }
}