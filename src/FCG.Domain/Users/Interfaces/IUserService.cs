namespace FCG.Domain.Users.Interfaces;

public interface IUserService
{
    Task CheckEmailUniquenessAsync(string email, CancellationToken cancellationToken = default);
}