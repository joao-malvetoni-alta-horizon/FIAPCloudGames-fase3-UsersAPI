using FCG.Domain.Shared;

namespace FCG.Domain.Users.Exceptions;

public sealed class UserNotFoundException(Guid id)
    : DomainException($"User with id '{id}' was not found.");