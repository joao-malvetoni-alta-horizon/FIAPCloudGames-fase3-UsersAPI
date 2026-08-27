namespace FCG.Domain.Users.Exceptions;

public class UserAlreadyExistsException(string email)
    : UserDomainException($"Usuário com o e-mail '{email}' já cadastrado.");