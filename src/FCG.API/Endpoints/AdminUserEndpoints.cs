using FCG.Application.Users.DTOs;
using FCG.Application.Users.Interfaces;

namespace FCG.API.Endpoints;

public static class AdminUserEndpoints
{
    public static void MapAdminUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/users")
            .WithTags("Admin - Users")
            .RequireAuthorization("AdminOnly");

        group.MapPost("/", CreateUser)
            .WithName("AdminCreateUser")
            .Produces<AdminCreateUserResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        group.MapGet("/", ListUsers)
            .WithName("AdminListUsers")
            .Produces<PagedUsersResponse>();

        group.MapGet("/{id:guid}", GetUserDetail)
            .WithName("AdminGetUserDetail")
            .Produces<UserDetailResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateUser)
            .WithName("AdminUpdateUser")
            .Produces<AdminUpdateUserResponse>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteUser)
            .WithName("AdminDeleteUser")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> CreateUser(
        AdminCreateUserRequest request,
        IAdminCreateUserUseCase useCase,
        CancellationToken ct)
    {
        var response = await useCase.ExecuteAsync(request, ct);
        return Results.Created($"/api/admin/users/{response.Id}", response);
    }

    private static async Task<IResult> ListUsers(
        IListUsersUseCase useCase,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var response = await useCase.ExecuteAsync(page, pageSize, ct);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetUserDetail(
        Guid id,
        IGetUserDetailUseCase useCase,
        CancellationToken ct)
    {
        var response = await useCase.ExecuteAsync(id, ct);
        return Results.Ok(response);
    }

    private static async Task<IResult> UpdateUser(
        Guid id,
        AdminUpdateUserRequest request,
        IAdminUpdateUserUseCase useCase,
        CancellationToken ct)
    {
        var response = await useCase.ExecuteAsync(id, request, ct);
        return Results.Ok(response);
    }

    private static async Task<IResult> DeleteUser(
        Guid id,
        IAdminDeleteUserUseCase useCase,
        CancellationToken ct)
    {
        await useCase.ExecuteAsync(id, ct);
        return Results.NoContent();
    }
}