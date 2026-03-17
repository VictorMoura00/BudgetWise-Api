using BudgetWise.Application.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetWise.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // ----- Custom business columns -----

        builder.Property(u => u.FullName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        // ----- Identity column overrides -----

        builder.Property(u => u.Email)
            .HasColumnType("citext")
            .IsRequired();

        builder.Property(u => u.NormalizedEmail)
            .HasColumnType("citext");

        builder.Property(u => u.UserName)
            .HasColumnType("citext");

        builder.Property(u => u.NormalizedUserName)
            .HasColumnType("citext");

        // ----- Ignore unused Identity columns -----

        builder.Ignore(u => u.PhoneNumber);
        builder.Ignore(u => u.PhoneNumberConfirmed);
        builder.Ignore(u => u.TwoFactorEnabled);
        builder.Ignore(u => u.LockoutEnabled);
        builder.Ignore(u => u.LockoutEnd);
        builder.Ignore(u => u.AccessFailedCount);
    }
}