using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FCG.Application.Auth.DTOs;
using FCG.Application.Users.DTOs;
using FCG.Domain.Users.Enums;
using Shouldly;

namespace FCG.IntegrationTests;

public class UsersApiTests(UsersApiFactory factory) : IClassFixture<UsersApiFactory>
{
    private const string SeededAdminEmail = "admin@fcg.com";
    private const string SeededAdminPassword = "Admin@123";

    // Espelha a serialização da API (enums como string) para desserializar as respostas.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly UsersApiFactory _factory = factory;

    private static string UniqueEmail()
    {
        return $"user-{Guid.NewGuid():N}@example.com";
    }

    [Fact]
    public async Task PostRegister_WhenRequestIsValid_ShouldReturnCreatedUser()
    {
        var client = _factory.CreateClient();
        var email = UniqueEmail();

        var response = await client.PostAsJsonAsync("/api/users/register",
            new { name = "Integration User", email, password = "Strong@123" });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<RegisterUserResponse>(JsonOptions);
        payload.ShouldNotBeNull();
        payload.Email.ShouldBe(email);
        payload.Role.ShouldBe(RoleType.User);
    }

    [Fact]
    public async Task PostRegister_WhenEmailAlreadyExists_ShouldReturnConflict()
    {
        var client = _factory.CreateClient();
        var email = UniqueEmail();
        var body = new { name = "Dup User", email, password = "Strong@123" };

        var first = await client.PostAsJsonAsync("/api/users/register", body);
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/api/users/register", body);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PostRegister_WhenPasswordIsWeak_ShouldReturnBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/register",
            new { name = "Weak User", email = UniqueEmail(), password = "abc" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostLogin_WhenSeededAdminCredentials_ShouldReturnToken()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = SeededAdminEmail, password = SeededAdminPassword });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        payload.ShouldNotBeNull();
        payload.AccessToken.ShouldNotBeNullOrEmpty();
        payload.TokenType.ShouldBe("Bearer");
    }

    [Fact]
    public async Task PostLogin_WhenPasswordIsWrong_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = SeededAdminEmail, password = "WrongPass@1" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAdminUsers_WithoutToken_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/users");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAdminUsers_WithAdminToken_ShouldReturnOk()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsAdminAsync(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/admin/users");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<PagedUsersResponse>();
        payload.ShouldNotBeNull();
    }

    [Fact]
    public async Task PostAdminUser_WithValidRole_ShouldReturnCreatedUserWithRole()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsAdminAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/admin/users",
            new { name = "Created Admin", email = UniqueEmail(), password = "Strong@123", role = "Administrator" });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<AdminCreateUserResponse>(JsonOptions);
        payload!.Role.ShouldBe(RoleType.Administrator);
    }

    [Fact]
    public async Task PostAdminUser_WithInvalidRole_ShouldReturnBadRequest()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsAdminAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/admin/users",
            new { name = "Bad Role", email = UniqueEmail(), password = "Strong@123", role = "Wizard" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterThenLogin_ShouldAuthenticateNewUser()
    {
        var client = _factory.CreateClient();
        var email = UniqueEmail();
        const string password = "Strong@123";

        var register = await client.PostAsJsonAsync("/api/users/register",
            new { name = "Flow User", email, password });
        register.StatusCode.ShouldBe(HttpStatusCode.Created);

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await login.Content.ReadFromJsonAsync<LoginResponse>();
        payload!.AccessToken.ShouldNotBeNullOrEmpty();
    }

    private static async Task<string> LoginAsAdminAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = SeededAdminEmail, password = SeededAdminPassword });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return payload!.AccessToken;
    }
}