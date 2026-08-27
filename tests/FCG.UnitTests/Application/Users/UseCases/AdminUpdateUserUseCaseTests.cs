using FCG.Application.Users.DTOs;
using FCG.Application.Users.UseCases;
using FCG.Domain.Users.Constants;
using FCG.Domain.Users.Entities;
using FCG.Domain.Users.Enums;
using FCG.Domain.Users.Exceptions;
using FCG.Domain.Users.Interfaces;
using NSubstitute;
using Shouldly;

namespace FCG.UnitTests.Application.Users.UseCases;

public class AdminUpdateUserUseCaseTests
{
    private readonly IUserUnitOfWork _unitOfWork = Substitute.For<IUserUnitOfWork>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();

    private AdminUpdateUserUseCase CreateUseCase()
    {
        _unitOfWork.Users.Returns(_userRepo);
        return new AdminUpdateUserUseCase(_unitOfWork);
    }

    private static User MakeUser()
    {
        return User.Create("Test User", "test@example.com", "hash", RoleType.User);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserNotFound_ShouldThrowUserNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _userRepo
            .GetByIdAsync(missingId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var useCase = CreateUseCase();

        var act = async () => await useCase.ExecuteAsync(missingId, new AdminUpdateUserRequest(null, null));

        await Should.ThrowAsync<UserNotFoundException>(act);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTargetIsRootAdmin_ShouldThrowRootAdminOperationForbiddenException()
    {
        var rootAdmin = User.CreateRootAdmin("Root", "root@example.com", "hash", RoleType.Administrator);
        _userRepo
            .GetByIdAsync(UserSeedConstants.RootAdminId, Arg.Any<CancellationToken>())
            .Returns(rootAdmin);

        var useCase = CreateUseCase();

        var act = async () =>
            await useCase.ExecuteAsync(UserSeedConstants.RootAdminId, new AdminUpdateUserRequest(false, null));

        await Should.ThrowAsync<RootAdminOperationForbiddenException>(act);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithIsActiveFalse_ShouldDeactivateUser()
    {
        var user = MakeUser();
        _userRepo
            .GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var useCase = CreateUseCase();
        var response = await useCase.ExecuteAsync(user.Id, new AdminUpdateUserRequest(false, null));

        response.IsActive.ShouldBeFalse();
        _userRepo.Received(1).Update(Arg.Any<User>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithIsActiveTrue_ShouldActivateUser()
    {
        var user = MakeUser();
        user.Deactivate();
        _userRepo
            .GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var useCase = CreateUseCase();
        var response = await useCase.ExecuteAsync(user.Id, new AdminUpdateUserRequest(true, null));

        response.IsActive.ShouldBeTrue();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithNewRole_ShouldChangeUserRole()
    {
        var user = MakeUser();
        _userRepo
            .GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var useCase = CreateUseCase();
        var response = await useCase.ExecuteAsync(user.Id, new AdminUpdateUserRequest(null, RoleType.Administrator));

        response.Role.ShouldBe(RoleType.Administrator);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithNullFields_ShouldNotChangeUserAndCommit()
    {
        var user = MakeUser();
        _userRepo
            .GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var useCase = CreateUseCase();
        var response = await useCase.ExecuteAsync(user.Id, new AdminUpdateUserRequest(null, null));

        response.IsActive.ShouldBeTrue();
        response.Role.ShouldBe(RoleType.User);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
