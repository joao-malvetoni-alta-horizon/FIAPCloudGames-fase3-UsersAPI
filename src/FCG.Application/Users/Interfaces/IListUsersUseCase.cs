using FCG.Application.Users.DTOs;

namespace FCG.Application.Users.Interfaces;

public interface IListUsersUseCase
{
    Task<PagedUsersResponse> ExecuteAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}