using FCG.Application.Users.DTOs;

namespace FCG.Application.Users.Interfaces;

public interface IAdminCreateUserUseCase
{
    Task<AdminCreateUserResponse> ExecuteAsync(AdminCreateUserRequest request,
        CancellationToken cancellationToken = default);
}