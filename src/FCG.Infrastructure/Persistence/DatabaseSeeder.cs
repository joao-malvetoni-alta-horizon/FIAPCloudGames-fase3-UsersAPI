using FCG.Domain.Users.Constants;
using FCG.Domain.Users.Entities;
using FCG.Domain.Users.Enums;
using FCG.Domain.Users.Interfaces;
using FCG.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FCG.Infrastructure.Persistence;

public class DatabaseSeeder(AppDbContext db, IPasswordHasher passwordHasher, ILogger<DatabaseSeeder> logger)
{
    private const string AdminEmail = "admin@fcg.com";
    private const string AdminPassword = "Admin@123";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var adminExists = await db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Id == UserSeedConstants.RootAdminId, cancellationToken);

        if (adminExists) return;

        var passwordHash = passwordHasher.Hash(AdminPassword);
        var admin = User.CreateRootAdmin("Administrador", AdminEmail, passwordHash, RoleType.Administrator);

        db.Users.Add(admin);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Admin criado — email: {Email} | senha: {Password}", AdminEmail, AdminPassword);
    }
}