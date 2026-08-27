using FCG.Application.Messaging;
using FCG.Application.Users.DTOs;
using FCG.Application.Users.Interfaces;
using FCG.Domain.Users.Entities;
using FCG.Domain.Users.Enums;
using FCG.Domain.Users.Interfaces;
using FCG.Domain.Users.ValueObjects;
using FiapCloudGames.Contracts.Users;

namespace FCG.Application.Users.UseCases;

public class RegisterUserUseCase(
    IUserUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IUserService userService,
    IIntegrationEventPublisher eventPublisher)
    : IRegisterUserUseCase
{
    public async Task<RegisterUserResponse> ExecuteAsync(RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        await userService.CheckEmailUniquenessAsync(request.Email, cancellationToken);

        Password.Validate(request.Password);
        var passwordHash = passwordHasher.Hash(request.Password);
        var user = User.Create(request.Name, request.Email, passwordHash, RoleType.User);

        await unitOfWork.Users.AddAsync(user, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        var userRegisteredEvent = new UserRegisteredEvent(user.Id, user.Name.Value, user.Email.Address);
        await eventPublisher.PublishAsync(userRegisteredEvent, cancellationToken);

        return new RegisterUserResponse(user.Id, user.Name.Value, user.Email.Address, user.Role);
    }
}