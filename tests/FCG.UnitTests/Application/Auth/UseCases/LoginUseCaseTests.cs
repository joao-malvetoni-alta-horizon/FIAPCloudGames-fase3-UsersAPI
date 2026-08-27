using FCG.Application.Auth.DTOs;
using FCG.Application.Auth.Interfaces;
using FCG.Application.Auth.UseCases;
using FCG.Domain.Users.Entities;
using FCG.Domain.Users.Enums;
using FCG.Domain.Users.Exceptions;
using FCG.Domain.Users.Interfaces;
using NSubstitute;
using Shouldly;

namespace FCG.UnitTests.Application.Auth.UseCases;

public class LoginUseCaseTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly LoginUseCase _sut;

    public LoginUseCaseTests()
    {
        _sut = new LoginUseCase(_userRepository, _passwordHasher, _jwtTokenService);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserNotFound_ShouldThrowInvalidCredentialsException()
    {
        _userRepository
            .GetByEmailAsync("unknown@test.com", Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var act = async () =>
            await _sut.ExecuteAsync(new LoginRequest("unknown@test.com", "anypass"), CancellationToken.None);

        await Should.ThrowAsync<InvalidCredentialsException>(act);
        _jwtTokenService.DidNotReceive().GenerateToken(Arg.Any<User>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserIsInactive_ShouldThrowInvalidCredentialsException()
    {
        var user = CreateInactiveUser();
        _userRepository
            .GetByEmailAsync(user.Email.Address, Arg.Any<CancellationToken>())
            .Returns(user);

        var act = async () =>
            await _sut.ExecuteAsync(new LoginRequest(user.Email.Address, "anypass"), CancellationToken.None);

        await Should.ThrowAsync<InvalidCredentialsException>(act);
        _jwtTokenService.DidNotReceive().GenerateToken(Arg.Any<User>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenPasswordIsWrong_ShouldThrowInvalidCredentialsException()
    {
        var user = CreateActiveUser();
        _userRepository
            .GetByEmailAsync(user.Email.Address, Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher
            .Verify("wrongpass", user.PasswordHash)
            .Returns(false);

        var act = async () =>
            await _sut.ExecuteAsync(new LoginRequest(user.Email.Address, "wrongpass"), CancellationToken.None);

        await Should.ThrowAsync<InvalidCredentialsException>(act);
        _jwtTokenService.DidNotReceive().GenerateToken(Arg.Any<User>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenCredentialsAreValid_ShouldReturnLoginResponse()
    {
        var user = CreateActiveUser();
        _userRepository
            .GetByEmailAsync(user.Email.Address, Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher
            .Verify("correct@123", user.PasswordHash)
            .Returns(true);
        _jwtTokenService
            .GenerateToken(user)
            .Returns("jwt-token");

        var response =
            await _sut.ExecuteAsync(new LoginRequest(user.Email.Address, "correct@123"), CancellationToken.None);

        response.AccessToken.ShouldBe("jwt-token");
        response.TokenType.ShouldBe("Bearer");
        response.ExpiresIn.ShouldBe(4 * 3600);
        _jwtTokenService.Received(1).GenerateToken(user);
    }

    private static User CreateActiveUser()
    {
        return User.Create("Test User", "test@fcg.com", "$2a$12$somehashvalue", RoleType.User);
    }

    private static User CreateInactiveUser()
    {
        var user = User.Create("Inactive User", "inactive@fcg.com", "$2a$12$somehashvalue", RoleType.User);
        user.Deactivate();
        return user;
    }
}