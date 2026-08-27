using System.Text.RegularExpressions;
using FCG.Domain.Users.Exceptions;

namespace FCG.Domain.Users.ValueObjects;

public sealed class Email
{
    public const int MaxLength = 320;
    private const int MinLength = 5;

    private static readonly Regex Regex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(250));

    public string Address { get; }

    private Email(string address)
    {
        Address = address;
    }

    public static Email FromStorage(string raw)
    {
        return new Email(raw);
    }

    public static Email Create(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new UserDomainException("E-mail address cannot be null or empty.");
        var normalized = address.Trim();
        if (normalized.Length is > MaxLength or < MinLength)
            throw new UserDomainException($"E-mail address must be between {MinLength} and {MaxLength} characters.");

        return !Regex.IsMatch(normalized)
            ? throw new UserDomainException($"E-mail address '{normalized}' has an invalid format.")
            : new Email(normalized.ToLowerInvariant());
    }

    public override bool Equals(object? obj)
    {
        return obj is Email other && string.Equals(Address, other.Address, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Address);
    }

    public override string ToString()
    {
        return Address;
    }

    public static implicit operator string(Email email)
    {
        return email.Address;
    }

    public static implicit operator Email(string address)
    {
        return Create(address);
    }
}