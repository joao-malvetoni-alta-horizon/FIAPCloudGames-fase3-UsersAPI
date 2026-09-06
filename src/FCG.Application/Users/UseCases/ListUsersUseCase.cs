using FCG.Application.Shared.Cache;
using FCG.Application.Users.DTOs;
using FCG.Application.Users.Interfaces;
using FCG.Domain.Users.Enums;
using FCG.Domain.Users.Interfaces;

namespace FCG.Application.Users.UseCases;

public class ListUsersUseCase(IUserRepository userRepository, ICacheService cacheService) : IListUsersUseCase
{
    public async Task<PagedUsersResponse> ExecuteAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 10) pageSize = 10;

        var cacheKey = $"users:all:page={page}:size={pageSize}";

        var pagedUsersResponse = await cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var (items, totalCount) = await userRepository.ListAsync(page, pageSize, cancellationToken);

                var summaries = items
                    .Select(u => new UserSummaryResponse(u.Id, u.Name.Value, u.Email.Address, u.Role.DisplayName))
                    .ToList();

                return new PagedUsersResponse(summaries, totalCount, page, pageSize);
            },
            TimeSpan.FromMinutes(3),
            cancellationToken
        );

        if (pagedUsersResponse is null)
        {
            return new PagedUsersResponse(new List<UserSummaryResponse>(), 0, 0, 0);
        }

        return pagedUsersResponse!;

        
    }
}