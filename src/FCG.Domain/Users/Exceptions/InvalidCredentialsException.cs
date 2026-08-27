using FCG.Domain.Shared;

namespace FCG.Domain.Users.Exceptions;

public sealed class InvalidCredentialsException() : DomainException("Invalid credentials.");