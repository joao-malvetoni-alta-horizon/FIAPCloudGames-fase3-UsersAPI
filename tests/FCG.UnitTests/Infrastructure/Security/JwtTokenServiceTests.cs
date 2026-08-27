using System.Text;
using System.Text.Json;
using FCG.Domain.Users.Entities;
using FCG.Domain.Users.Enums;
using FCG.Infrastructure.Security;
using Shouldly;

namespace FCG.UnitTests.Infrastructure.Security;

public class JwtTokenServiceTests
{
    private readonly JwtSettings _settings = new()
    {
        SecretKey = "test-secret-key-with-at-least-32-chars!!",
        ExpirationHours = 1
    };

    [Fact]
    public void GenerateToken_ShouldReturnValidJwtFormat()
    {
        var service = new JwtTokenService(_settings);
        var user = CreateUser(RoleType.User);

        var token = service.GenerateToken(user);

        token.ShouldNotBeNullOrEmpty();
        token.Split('.').Length.ShouldBe(3, "JWT must have header.payload.signature format");
    }

    [Fact]
    public void GenerateToken_ForUserRole_ShouldContainCorrectRoleClaim()
    {
        var service = new JwtTokenService(_settings);
        var user = CreateUser(RoleType.User);

        var token = service.GenerateToken(user);
        var payload = DecodePayload(token);

        payload.ShouldContain(RoleType.User.DisplayName);
        payload.ShouldContain(user.Id.ToString());
        payload.ShouldContain(user.Email.Address);
    }

    [Fact]
    public void GenerateToken_ForAdminRole_ShouldContainAdminRoleClaim()
    {
        var service = new JwtTokenService(_settings);
        var admin = CreateUser(RoleType.Administrator);

        var token = service.GenerateToken(admin);
        var payload = DecodePayload(token);

        payload.ShouldContain(RoleType.Administrator.DisplayName);
    }

    [Fact]
    public void GenerateToken_ShouldSetExpirationBasedOnSettings()
    {
        var service = new JwtTokenService(_settings);
        var user = CreateUser(RoleType.User);

        var before = DateTimeOffset.UtcNow;
        var token = service.GenerateToken(user);
        var after = DateTimeOffset.UtcNow;

        var payload = DecodePayload(token);
        var doc = JsonDocument.Parse(payload);
        var exp = doc.RootElement.GetProperty("exp").GetInt64();
        var expTime = DateTimeOffset.FromUnixTimeSeconds(exp);

        expTime.ShouldBeGreaterThan(before.AddHours(1).AddSeconds(-5));
        expTime.ShouldBeLessThan(after.AddHours(1).AddSeconds(5));
    }

    private static User CreateUser(RoleType roleType)
    {
        return User.Create("Test User", "test@fcg.com", "$2a$12$somehashvalue", roleType);
    }

    private static string DecodePayload(string token)
    {
        var part = token.Split('.')[1];
        part = part.Replace('-', '+').Replace('_', '/');
        var mod4 = part.Length % 4;
        if (mod4 != 0) part += new string('=', 4 - mod4);
        return Encoding.UTF8.GetString(Convert.FromBase64String(part));
    }
}