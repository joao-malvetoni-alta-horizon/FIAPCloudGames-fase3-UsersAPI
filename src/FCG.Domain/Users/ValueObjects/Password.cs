using FCG.Domain.Users.Exceptions;

namespace FCG.Domain.Users.ValueObjects;

public static class Password
{
    private const int MinLength = 8;
    private const string SpecialChars = "!@#$%^&*()-_+=";

    public static void Validate(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new UserDomainException("Password cannot be null or empty.");

        if (plainText.Length < MinLength)
            throw new UserDomainException($"Password must be at least {MinLength} characters long.");

        if (!plainText.Any(char.IsUpper))
            throw new UserDomainException("Password must contain at least one uppercase letter.");

        if (!plainText.Any(char.IsLower))
            throw new UserDomainException("Password must contain at least one lowercase letter.");

        if (!plainText.Any(char.IsDigit))
            throw new UserDomainException("Password must contain at least one digit.");

        if (!plainText.Any(c => SpecialChars.Contains(c)))
            throw new UserDomainException($"Password must contain at least one special character ({SpecialChars}).");
    }
}