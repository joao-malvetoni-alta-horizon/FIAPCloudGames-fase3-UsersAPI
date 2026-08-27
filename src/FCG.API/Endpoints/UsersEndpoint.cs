using FCG.Application.Users.DTOs;
using FCG.Application.Users.Interfaces;

namespace FCG.API.Endpoints;

public static class UsersEndpoint
{
    public static void MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users");

        group.MapPost("/register", RegisterUser)
            .WithName("RegisterUser")
            .Produces<RegisterUserResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> RegisterUser(
        RegisterUserRequest request,
        IRegisterUserUseCase useCase,
        CancellationToken ct
    )

    {
        var response = await useCase.ExecuteAsync(request, ct);
        return Results.Created($"/api/users/register", response);
    }
}