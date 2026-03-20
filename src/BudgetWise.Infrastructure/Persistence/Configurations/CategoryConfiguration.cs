using BudgetWise.Domain.Entities;
using BudgetWise.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetWise.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.Name)
            .HasColumnType("citext")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasColumnType("text");

        builder.Property(c => c.Icon)
            .HasMaxLength(50);

        builder.Property(c => c.Color)
            .HasMaxLength(7)
                .HasConversion(
                color => color == null ? null : color.Value,
                value => string.IsNullOrEmpty(value)
                    ? null
                    : HexColor.Create(value).IsSuccess
                        ? HexColor.Create(value).Value
                        : null);

        builder.Property(c => c.IsSystem)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(c => c.UserId);

        builder.HasIndex(c => new { c.Name, c.UserId })
            .IsUnique()
            .HasDatabaseName("ix_categories_name_user_id");

        builder.HasIndex(c => c.UserId)
            .HasFilter("is_active = true")
            .HasDatabaseName("ix_categories_user_active");

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_categories_color",
            "color ~ '^#[0-9A-Fa-f]{6}$' OR color IS NULL"));
    }
}
