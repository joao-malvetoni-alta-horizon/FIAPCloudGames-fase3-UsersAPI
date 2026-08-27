using FCG.Domain.Users.Exceptions;
using FCG.Domain.Users.ValueObjects;
using Shouldly;

namespace FCG.UnitTests.Domain.ValueObjects;

public class NameTests
{
    [Fact]
    public void Create_ValidName_ShouldReturnName()
    {
        var name = Name.Create("John Doe");
        name.Value.ShouldBe("John Doe");
    }

    [Fact]
    public void Create_EmptyName_ShouldThrowUserDomainException()
    {
        var ex = Should.Throw<UserDomainException>(() => Name.Create(string.Empty));
        ex.Message.ShouldContain("cannot be null or empty");
    }

    [Fact]
    public void Create_WhitespaceName_ShouldThrowUserDomainException()
    {
        var ex = Should.Throw<UserDomainException>(() => Name.Create("   "));
        ex.Message.ShouldContain("cannot be null or empty");
    }

    [Fact]
    public void Create_NameWithLeadingAndTrailingSpaces_ShouldTrim()
    {
        var name = Name.Create("  John Doe  ");
        name.Value.ShouldBe("John Doe");
    }

    [Fact]
    public void Create_NameShorterThanMinLengthAfterTrim_ShouldThrowUserDomainException()
    {
        var ex = Should.Throw<UserDomainException>(() => Name.Create(" a "));
        ex.Message.ShouldContain($"between {Name.MinLength} and {Name.MaxLength}");
    }

    [Fact]
    public void Create_NullName_ShouldThrowUserDomainException()
    {
        var ex = Should.Throw<UserDomainException>(() => Name.Create(null));
        ex.Message.ShouldContain("cannot be null or empty");
    }

    [Fact]
    public void Names_WithSameNormalizedValue_ShouldBeEqual()
    {
        var first = Name.Create("John Doe");
        var second = Name.Create("  John Doe  ");
        first.ShouldBe(second);
    }
}