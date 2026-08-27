using FCG.Application.Users.UseCases;
using FCG.Domain.Users.Constants;
using FCG.Domain.Users.Entities;
using FCG.Domain.Users.Enums;
using FCG.Domain.Users.Exceptions;
using FCG.Domain.Users.Interfaces;
using NSubstitute;
using Shouldly;

namespace FCG.UnitTests.Application.Users.UseCases;

public class AdminDeleteUserUseCaseTests
{
    private readonly IUserUnitOfWork _unitOfWork = Substitute.For<IUserUnitOfWork>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();

    private AdminDeleteUserUseCase CreateUseCase()
    {
        _unitOfWork.Users.Returns(_userRepo);
        return new AdminDeleteUserUseCase(_unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserNotFound_ShouldThrowUserNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _userRepo
            .GetByIdAsync(missingId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var useCase = CreateUseCase();

        var act = async () => await useCase.ExecuteAsync(missingId);

        await Should.ThrowAsync<UserNotFoundException>(act);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenTargetIsRootAdmin_ShouldThrowRootAdminOperationForbiddenException()
    {
        var rootAdmin = User.CreateRootAdmin("Root", "root@example.com", "hash", RoleType.Administrator);
        _userRepo
            .GetByIdAsync(UserSeedConstants.RootAdminId, Arg.Any<CancellationToken>())
            .Returns(rootAdmin);

        var useCase = CreateUseCase();

        var act = async () => await useCase.ExecuteAsync(UserSeedConstants.RootAdminId);

        await Should.ThrowAsync<RootAdminOperationForbiddenException>(act);
        _userRepo.DidNotReceive().Update(Arg.Any<User>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserExists_ShouldSoftDeleteAndCommit()
    {
        var user = User.Create("Some User", "some@example.com", "hash", RoleType.User);
        _userRepo
            .GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var useCase = CreateUseCase();
        await useCase.ExecuteAsync(user.Id);

        user.IsActive.ShouldBeFalse();
        user.DeletedAt.ShouldNotBeNull();
        _userRepo.Received(1).Update(user);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}