using FCG.Application.Users.DTOs;
using FCG.Application.Users.Interfaces;
using FCG.Domain.Users.Enums;
using FCG.Domain.Users.Exceptions;
using FCG.Domain.Users.Interfaces;

namespace FCG.Application.Users.UseCases;

public class GetUserDetailUseCase(IUserRepository userRepository) : IGetUserDetailUseCase
{
    public async Task<UserDetailResponse> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
                   ?? throw new UserNotFoundException(userId);

        return new UserDetailResponse(user.Id, user.Name.Value, user.Email.Address, user.Role.DisplayName,
            user.IsActive);
    }
}