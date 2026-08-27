using FCG.Domain.Users.Entities;
using FCG.Domain.Users.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCG.Infrastructure.Persistence.Mappings;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Name)
            .HasColumnName("Name")
            .HasMaxLength(Name.MaxLength)
            .IsRequired()
            .HasConversion(
                name => name.Value,
                raw => Name.FromStorage(raw));

        builder.Property(u => u.Email)
            .HasColumnName("Email")
            .HasMaxLength(Email.MaxLength)
            .IsRequired()
            .HasConversion(
                email => email.Address,
                raw => Email.FromStorage(raw));

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.IsActive).IsRequired();
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt);
        builder.Property(u => u.DeletedAt);

        builder.HasQueryFilter(u => u.DeletedAt == null);
    }
}