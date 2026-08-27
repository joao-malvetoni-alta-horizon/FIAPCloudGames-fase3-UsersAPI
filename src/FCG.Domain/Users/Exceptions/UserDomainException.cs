using FCG.Domain.Shared;

namespace FCG.Domain.Users.Exceptions;

public class UserDomainException(string message) : DomainException(message);