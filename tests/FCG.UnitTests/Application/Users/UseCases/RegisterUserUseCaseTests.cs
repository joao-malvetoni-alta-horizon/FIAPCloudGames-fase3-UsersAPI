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

public class RegisterUserUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenEmailAlreadyExists_ShouldThrowUserAlreadyExistsException()
    {
        // Arrange
        var unitOfWork = Substitute.For<IUserUnitOfWork>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var userService = Substitute.For<IUserService>();
        var eventPublisher = Substitute.For<IIntegrationEventPublisher>();
        userService
            .CheckEmailUniquenessAsync("existing@example.com", Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new UserAlreadyExistsException("existing@example.com")));

        var useCase = new RegisterUserUseCase(unitOfWork, passwordHasher, userService, eventPublisher);
        var request = new RegisterUserRequest("Existing User", "existing@example.com", "Valid@123");

        // Act
        var act = async () => await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        await Should.ThrowAsync<UserAlreadyExistsException>(act);
        await eventPublisher.DidNotReceive().PublishAsync(
            Arg.Any<UserRegisteredEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenPasswordIsInvalid_ShouldThrowUserDomainException()
    {
        // Arrange
        var usersRepository = Substitute.For<IUserRepository>();
        var unitOfWork = Substitute.For<IUserUnitOfWork>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var userService = Substitute.For<IUserService>();
        var eventPublisher = Substitute.For<IIntegrationEventPublisher>();
        unitOfWork.Users.Returns(usersRepository);

        var useCase = new RegisterUserUseCase(unitOfWork, passwordHasher, userService, eventPublisher);
        var request = new RegisterUserRequest("Weak Password User", "weak@example.com", "abc");

        // Act
        var act = async () => await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        await Should.ThrowAsync<UserDomainException>(act);
        passwordHasher.DidNotReceive().Hash(Arg.Any<string>());
        await usersRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenRequestIsValid_ShouldAddUserAndCommit()
    {
        // Arrange
        var usersRepository = Substitute.For<IUserRepository>();
        var unitOfWork = Substitute.For<IUserUnitOfWork>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var userService = Substitute.For<IUserService>();
        var eventPublisher = Substitute.For<IIntegrationEventPublisher>();
        unitOfWork.Users.Returns(usersRepository);
        passwordHasher.Hash("Strong@123").Returns("hashed-password");

        var useCase = new RegisterUserUseCase(unitOfWork, passwordHasher, userService, eventPublisher);
        var request = new RegisterUserRequest("New User", "new@example.com", "Strong@123");

        // Act
        var response = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        response.Email.ShouldBe("new@example.com");
        response.Name.ShouldBe("New User");
        response.Role.ShouldBe(RoleType.User);
        await usersRepository.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await eventPublisher.Received(1).PublishAsync(
            Arg.Is<UserRegisteredEvent>(e => e.Email == "new@example.com" && e.UserId == response.Id),
            Arg.Any<CancellationToken>());
    }
}