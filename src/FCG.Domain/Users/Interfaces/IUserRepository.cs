using FCG.Domain.Users.Entities;
using FCG.Domain.Shared;

namespace FCG.Domain.Users.Interfaces;

/// <summary>Contrato de repositório para o agregado <see cref="User"/>.</summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>Busca um usuário pelo endereço de e-mail (sem distinção de maiúsculas/minúsculas).</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Retorna <c>true</c> se já existir um usuário com o e-mail informado.</summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Retorna uma página de usuários ativos ordenados por nome, junto com a contagem total.</summary>
    Task<(IReadOnlyList<User> Items, int TotalCount)> ListAsync(int page, int pageSize,
        CancellationToken cancellationToken = default);
}