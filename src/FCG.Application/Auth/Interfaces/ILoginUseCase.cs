using FCG.Application.Auth.DTOs;

namespace FCG.Application.Auth.Interfaces;

public interface ILoginUseCase
{
    Task<LoginResponse> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken = default);
}