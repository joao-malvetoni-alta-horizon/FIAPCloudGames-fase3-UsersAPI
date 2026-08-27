using FCG.Application.Messaging;
using FCG.Application.Users.DTOs;
using FCG.Application.Users.UseCases;
using FCG.Domain.Users.Entities;
using FCG.Domain.Users.Exceptions;
using FCG.Domain.Users.Interfaces;
using FiapCloudGames.Contracts.Users;
using NSubstitute;
using Shouldly;
using RoleType = FCG.Domain.Users.Enums.RoleType;

namespace FCG.UnitTests.Application.Users.UseCases;

public class AdminCreateUserUseCaseTests
{
    private readonly IUserUnitOfWork _unitOfWork = Substitute.For<IUserUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IIntegrationEventPublisher _eventPublisher = Substitute.For<IIntegrationEventPublisher>();

    private AdminCreateUserUseCase CreateUseCase()
    {
        _unitOfWork.Users.Returns(_userRepo);
        return new AdminCreateUserUseCase(_unitOfWork, _passwordHasher, _userService, _eventPublisher);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailAlreadyExists_ShouldThrowUserAlreadyExistsException()
    {
        _userService
            .CheckEmailUniquenessAsync("dup@example.com", Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new UserAlreadyExistsException("dup@example.com")));

        var useCase = CreateUseCase();
        var request =
            new AdminCreateUserRequest("Admin", "dup@example.com", "Valid@123", RoleType.Administrator);

        var act = async () => await useCase.ExecuteAsync(request);

        await Should.ThrowAsync<UserAlreadyExistsException>(act);
        await _userRepo.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenPasswordIsWeak_ShouldThrowUserDomainException()
    {
        var useCase = CreateUseCase();
        var request = new AdminCreateUserRequest("Admin", "admin@example.com", "weak", RoleType.Administrator);

        await Should.ThrowAsync<UserDomainException>(async () => await useCase.ExecuteAsync(request));
        _passwordHasher.DidNotReceive().Hash(Arg.Any<string>());
        await _userRepo.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenRequestIsValid_ShouldCreateUserAndCommit()
    {
        _passwordHasher
            .Hash("Valid@123")
            .Returns("hashed-value");

        var useCase = CreateUseCase();
        var request = new AdminCreateUserRequest("New Admin", "newadmin@example.com", "Valid@123", RoleType.Administrator);

        var response = await useCase.ExecuteAsync(request);

        response.ShouldNotBeNull();
        response.Name.ShouldBe("New Admin");
        response.Email.ShouldBe("newadmin@example.com");
        response.Role.ShouldBe(RoleType.Administrator);
        response.Id.ShouldNotBe(Guid.Empty);
        await _userRepo.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<UserRegisteredEvent>(e => e.Email == "newadmin@example.com" && e.UserId == response.Id),
            Arg.Any<CancellationToken>());
    }
}
