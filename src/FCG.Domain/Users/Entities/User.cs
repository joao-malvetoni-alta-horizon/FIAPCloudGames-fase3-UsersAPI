using FCG.Domain.Shared;
using FCG.Domain.Users.Constants;
using FCG.Domain.Users.Enums;
using FCG.Domain.Users.Exceptions;
using FCG.Domain.Users.ValueObjects;

namespace FCG.Domain.Users.Entities;

public class User : Entity
{
    public Name Name { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;

    public RoleType Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    protected User()
    {
    }

    private User(Name name, Email email, string passwordHash, RoleType role)
    {
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;
    }

    public static User Create(string name, string email, string passwordHash, RoleType role)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new UserDomainException("Password hash cannot be null or empty.");

        var emailVo = Email.Create(email);
        var nameVo = Name.Create(name);
        return new User(nameVo, emailVo, passwordHash, role);
    }

    public void UpdateName(string name)
    {
        Name = Name.Create(name);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateEmail(string email)
    {
        Email = Email.Create(email);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePassword(string newHash)
    {
        if (string.IsNullOrWhiteSpace(newHash))
            throw new UserDomainException("Password hash cannot be null or empty.");

        PasswordHash = newHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeRole(RoleType role)
    {
        Role = role;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsActive = false;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static User CreateRootAdmin(string name, string email, string passwordHash, RoleType role)
    {
        var admin = Create(name, email, passwordHash, role);
        admin.Id = UserSeedConstants.RootAdminId;
        return admin;
    }
}