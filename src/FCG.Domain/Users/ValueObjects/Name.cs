using FCG.Domain.Users.Exceptions;

namespace FCG.Domain.Users.ValueObjects;

public sealed record Name

{
    public const int MaxLength = 150;
    public const int MinLength = 2;

    public string Value { get; }

    public static Name Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new UserDomainException("Name cannot be null or empty.");

        var normalized = value.Trim();

        if (normalized.Length is > MaxLength or < MinLength)
            throw new UserDomainException($"Name must be between {MinLength} and {MaxLength} characters.");

        return new Name(normalized);
    }

    private Name(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(Name name)
    {
        return name.Value;
    }

    public static Name FromStorage(string raw)
    {
        return new Name(raw);
    }
}