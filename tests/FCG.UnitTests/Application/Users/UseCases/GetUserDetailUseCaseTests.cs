using FCG.Application.Users.UseCases;
using FCG.Domain.Users.Entities;
using FCG.Domain.Users.Enums;
using FCG.Domain.Users.Exceptions;
using FCG.Domain.Users.Interfaces;
using NSubstitute;
using Shouldly;

namespace FCG.UnitTests.Application.Users.UseCases;

public class GetUserDetailUseCaseTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();

    [Fact]
    public async Task ExecuteAsync_WhenUserNotFound_ShouldThrowUserNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _userRepo
            .GetByIdAsync(missingId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var useCase = new GetUserDetailUseCase(_userRepo);

        var act = async () => await useCase.ExecuteAsync(missingId);

        await Should.ThrowAsync<UserNotFoundException>(act);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserExists_ShouldReturnDetailResponse()
    {
        var user = User.Create("Detail User", "detail@example.com", "hash", RoleType.User);
        _userRepo
            .GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var useCase = new GetUserDetailUseCase(_userRepo);
        var response = await useCase.ExecuteAsync(user.Id);

        response.Id.ShouldBe(user.Id);
        response.Name.ShouldBe("Detail User");
        response.Email.ShouldBe("detail@example.com");
        response.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnRoleDisplayName()
    {
        var user = User.Create("Admin User", "adminuser@example.com", "hash", RoleType.Administrator);
        _userRepo
            .GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var useCase = new GetUserDetailUseCase(_userRepo);
        var response = await useCase.ExecuteAsync(user.Id);

        response.Role.ShouldBe(RoleType.Administrator.DisplayName);
    }
}