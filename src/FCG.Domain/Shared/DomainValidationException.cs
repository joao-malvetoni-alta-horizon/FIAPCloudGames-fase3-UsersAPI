namespace FCG.Domain.Shared;

public class DomainValidationException(string message) : DomainException(message);