using FCG.Domain.Users.Exceptions;
using FCG.Domain.Users.Interfaces;

namespace FCG.Domain.Users.Services;

public class UserService(IUserRepository userRepository) : IUserService
{
    public async Task CheckEmailUniquenessAsync(string email, CancellationToken cancellationToken = default)
    {
        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
            throw new UserAlreadyExistsException(email);
    }
}