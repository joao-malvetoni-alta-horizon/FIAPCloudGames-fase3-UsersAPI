using FCG.Domain.Users.Interfaces;
using FCG.Infrastructure.Persistence.Context;

namespace FCG.Infrastructure.Persistence;

public class UnitOfWork(
    AppDbContext context,
    IUserRepository users) : IUserUnitOfWork
{
    public IUserRepository Users { get; } = users;

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }
}