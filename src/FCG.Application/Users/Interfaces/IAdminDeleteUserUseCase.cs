namespace FCG.Application.Users.Interfaces;

public interface IAdminDeleteUserUseCase
{
    Task ExecuteAsync(Guid userId, CancellationToken cancellationToken = default);
}