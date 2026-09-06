using FCG.Application.Shared.Cache;
using FCG.Application.Users.Interfaces;
using FCG.Domain.Users.Constants;
using FCG.Domain.Users.Exceptions;
using FCG.Domain.Users.Interfaces;

namespace FCG.Application.Users.UseCases;

public class AdminDeleteUserUseCase(IUserUnitOfWork unitOfWork, ICacheService cacheService) : IAdminDeleteUserUseCase
{
    public async Task ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"user:{userId}";

        var user = await unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
                   ?? throw new UserNotFoundException(userId);

        if (user.Id == UserSeedConstants.RootAdminId)
            throw new RootAdminOperationForbiddenException();

        user.SoftDelete();
        unitOfWork.Users.Update(user);
        await unitOfWork.CommitAsync(cancellationToken);

        await cacheService.RemoveAsync(cacheKey, cancellationToken);
    }
}