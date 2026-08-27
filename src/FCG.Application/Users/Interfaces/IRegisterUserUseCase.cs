using FCG.Application.Users.DTOs;

namespace FCG.Application.Users.Interfaces;

public interface IRegisterUserUseCase
{
    Task<RegisterUserResponse> ExecuteAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);
}