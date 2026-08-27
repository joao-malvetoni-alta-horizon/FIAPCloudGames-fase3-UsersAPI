using FCG.Domain.Users.Constants;
using FCG.Domain.Users.Entities;
using FCG.Domain.Users.Enums;
using FCG.Domain.Users.Exceptions;
using Shouldly;

namespace FCG.UnitTests.Domain.Entities;

public class UserTests
{
    private const RoleType ValidRole = RoleType.User;
    private const string ValidEmail = "user@example.com";
    private const string ValidHash = "$2a$12$somehashvalue";

    [Fact]
    public void Create_ValidParams_ShouldReturnUser()
    {
        var user = User.Create("John Doe", ValidEmail, ValidHash, ValidRole);

        user.ShouldNotBeNull();
        user.Name.Value.ShouldBe("John Doe");
        user.Email.Address.ShouldBe(ValidEmail);
        user.PasswordHash.ShouldBe(ValidHash);
        user.Role.ShouldBe(ValidRole);
        user.IsActive.ShouldBeTrue();
        user.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Create_EmptyName_ShouldThrowUserDomainException()
    {
        var ex = Should.Throw<UserDomainException>(() => User.Create(string.Empty, ValidEmail, ValidHash, ValidRole));
        ex.Message.ShouldContain("ame cannot be null or empty");
    }

    [Fact]
    public void Create_WhitespaceName_ShouldThrowUserDomainException()
    {
        var ex = Should.Throw<UserDomainException>(() => User.Create("   ", ValidEmail, ValidHash, ValidRole));
        ex.Message.ShouldContain("ame cannot be null or empty");
    }

    [Fact]
    public void Create_InvalidEmail_ShouldThrowUserDomainException()
    {
        var ex = Should.Throw<UserDomainException>(() => User.Create("John", "notanemail", ValidHash, ValidRole));
        ex.Message.ShouldContain("invalid format");
    }

    [Fact]
    public void UpdateName_ValidName_ShouldChangeName()
    {
        var user = User.Create("John Doe", ValidEmail, ValidHash, ValidRole);
        user.UpdateName("Jane Doe");

        user.Name.Value.ShouldBe("Jane Doe");
        user.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var user = User.Create("John Doe", ValidEmail, ValidHash, ValidRole);
        user.Deactivate();

        user.IsActive.ShouldBeFalse();
        user.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        var user = User.Create("John Doe", ValidEmail, ValidHash, ValidRole);
        user.Deactivate();
        user.Activate();

        user.IsActive.ShouldBeTrue();
        user.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void SoftDelete_ShouldSetIsActiveFalseAndDeletedAt()
    {
        var user = User.Create("John Doe", ValidEmail, ValidHash, ValidRole);
        var before = DateTime.UtcNow;
        user.SoftDelete();

        user.IsActive.ShouldBeFalse();
        user.DeletedAt.ShouldNotBeNull();
        user.DeletedAt.Value.ShouldBeGreaterThanOrEqualTo(before);
        user.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void ChangeRole_ShouldUpdateRole()
    {
        var user = User.Create("John Doe", ValidEmail, ValidHash, ValidRole);

        user.ChangeRole(RoleType.Administrator);

        user.Role.ShouldBe(RoleType.Administrator);
        user.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void CreateRootAdmin_ShouldHaveRootAdminId()
    {
        var admin = User.CreateRootAdmin("Root Admin", "root@example.com", ValidHash, ValidRole);

        admin.Id.ShouldBe(UserSeedConstants.RootAdminId);
        admin.IsActive.ShouldBeTrue();
    }
}
