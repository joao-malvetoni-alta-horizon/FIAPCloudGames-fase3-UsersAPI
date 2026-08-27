using FCG.Domain.Shared;

namespace FCG.Domain.Users.Interfaces;

public interface IUserUnitOfWork : IUnitOfWork
{
    IUserRepository Users { get; }
}