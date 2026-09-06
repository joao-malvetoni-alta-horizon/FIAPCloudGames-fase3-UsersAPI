using FCG.Application.Shared.Cache;
using FCG.Application.Users.DTOs;
using FCG.Application.Users.Interfaces;
using FCG.Domain.Users.Enums;
using FCG.Domain.Users.Exceptions;
using FCG.Domain.Users.Interfaces;

namespace FCG.Application.Users.UseCases;

public class GetUserDetailUseCase(IUserRepository userRepository, ICacheService cacheService) : IGetUserDetailUseCase
{
    public async Task<UserDetailResponse> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"user:{userId}";

        var userDetailResponse = await cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var user = await userRepository.GetByIdAsync(userId, cancellationToken)
                       ?? throw new UserNotFoundException(userId);

                return new UserDetailResponse(user.Id, user.Name.Value, user.Email.Address, user.Role.DisplayName,
                    user.IsActive);
            },
            TimeSpan.FromMinutes(20),
            cancellationToken
        );

        return userDetailResponse!;

    }
}