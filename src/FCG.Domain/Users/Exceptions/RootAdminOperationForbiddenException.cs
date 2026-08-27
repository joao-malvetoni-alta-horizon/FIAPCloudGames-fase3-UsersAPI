namespace FCG.Domain.Users.Exceptions;

public class RootAdminOperationForbiddenException()
    : UserDomainException("The root administrator cannot be changed, disabled, or deleted.");