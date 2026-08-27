using FCG.Domain.Users.Exceptions;
using FCG.Domain.Users.ValueObjects;
using Shouldly;

namespace FCG.UnitTests.Domain.ValueObjects;

public class PasswordTests
{
    [Fact]
    public void Validate_ValidPassword_ShouldNotThrow()
    {
        Should.NotThrow(() => Password.Validate("StrongPass1!"));
    }

    [Fact]
    public void Validate_TooShort_ShouldThrowUserDomainException()
    {
        var ex = Should.Throw<UserDomainException>(() => Password.Validate("Ab1!"));
        ex.Message.ShouldContain("at least 8 characters");
    }

    [Fact]
    public void Validate_NoUppercase_ShouldThrowUserDomainException()
    {
        var ex = Should.Throw<UserDomainException>(() => Password.Validate("lowercase1!"));
        ex.Message.ShouldContain("uppercase");
    }

    [Fact]
    public void Validate_NoLowercase_ShouldThrowUserDomainException()
    {
        var ex = Should.Throw<UserDomainException>(() => Password.Validate("UPPERCASE1!"));
        ex.Message.ShouldContain("lowercase");
    }

    [Fact]
    public void Validate_NoDigit_ShouldThrowUserDomainException()
    {
        var ex = Should.Throw<UserDomainException>(() => Password.Validate("NoDigitPass!"));
        ex.Message.ShouldContain("digit");
    }

    [Fact]
    public void Validate_NoSpecialChar_ShouldThrowUserDomainException()
    {
        var ex = Should.Throw<UserDomainException>(() => Password.Validate("NoSpecial1A"));
        ex.Message.ShouldContain("special character");
    }

    [Fact]
    public void Validate_EmptyPassword_ShouldThrowUserDomainException()
    {
        var ex = Should.Throw<UserDomainException>(() => Password.Validate(string.Empty));
        ex.Message.ShouldContain("null or empty");
    }

    [Fact]
    public void Validate_NullPassword_ShouldThrowUserDomainException()
    {
        var ex = Should.Throw<UserDomainException>(() => Password.Validate(null!));
        ex.Message.ShouldContain("null or empty");
    }
}