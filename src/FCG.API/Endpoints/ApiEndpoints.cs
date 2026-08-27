namespace FCG.API.Endpoints;

public static class ApiEndpoints
{
    public static void MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapAuthEndpoints();
        app.MapUsersEndpoints();
        app.MapAdminUserEndpoints();
    }
}
