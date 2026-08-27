using FCG.Domain.Users.Exceptions;
using FCG.Domain.Users.ValueObjects;
using Shouldly;

namespace FCG.UnitTests.Domain.ValueObjects;

public class EmailTests
{
    [Fact]
    public void Create_ValidEmail_ShouldSucceed()
    {
        var email = Email.Create("user@example.com");
        email.Address.ShouldBe("user@example.com");
    }

    [Fact]
    public void Create_ValidEmailWithSubdomain_ShouldSucceed()
    {
        var email = Email.Create("user@mail.example.com");
        email.Address.ShouldBe("user@mail.example.com");
    }

    [Fact]
    public void Create_EmptyEmail_ShouldThrowUserDomainException()
    {
        var ex = Should.Throw<UserDomainException>(() => Email.Create(string.Empty));
        ex.Message.ShouldContain("null or empty");
    }

    [Fact]
    public void Create_NullEmail_ShouldThrowUserDomainException()
    {
        var ex = Should.Throw<UserDomainException>(() => Email.Create(null!));
        ex.Message.ShouldContain("null or empty");
    }

    [Fact]
    public void Create_EmailWithoutAt_ShouldThrowUserDomainException()
    {
        var ex = Should.Throw<UserDomainException>(() => Email.Create("userexample.com"));
        ex.Message.ShouldContain("invalid format");
    }

    [Fact]
    public void Create_EmailWithoutDomain_ShouldThrowUserDomainException()
    {
        var ex = Should.Throw<UserDomainException>(() => Email.Create("user@"));
        ex.Message.ShouldContain("invalid format");
    }

    [Fact]
    public void Create_EmailWithSpaces_ShouldThrowUserDomainException()
    {
        var ex = Should.Throw<UserDomainException>(() => Email.Create("user @example.com"));
        ex.Message.ShouldContain("invalid format");
    }

    [Fact]
    public void Equals_SameEmail_ShouldReturnTrue()
    {
        var e1 = Email.Create("user@example.com");
        var e2 = Email.Create("USER@EXAMPLE.COM");
        e1.ShouldBe(e2);
    }

    [Fact]
    public void Equals_DifferentEmail_ShouldReturnFalse()
    {
        var e1 = Email.Create("a@example.com");
        var e2 = Email.Create("b@example.com");
        e1.ShouldNotBe(e2);
    }

    [Fact]
    public void ImplicitConversion_StringToEmail_ShouldSucceed()
    {
        Email email = "user@example.com";
        string address = email;
        address.ShouldBe("user@example.com");
    }
}