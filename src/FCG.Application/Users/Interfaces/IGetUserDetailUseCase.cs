using FCG.Application.Users.DTOs;

namespace FCG.Application.Users.Interfaces;

public interface IGetUserDetailUseCase
{
    Task<UserDetailResponse> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default);
}