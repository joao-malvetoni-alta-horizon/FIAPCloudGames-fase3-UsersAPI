using FCG.Application.Users.DTOs;

namespace FCG.Application.Users.Interfaces;

public interface IAdminUpdateUserUseCase
{
    Task<AdminUpdateUserResponse> ExecuteAsync(Guid userId, AdminUpdateUserRequest request,
        CancellationToken cancellationToken = default);
}