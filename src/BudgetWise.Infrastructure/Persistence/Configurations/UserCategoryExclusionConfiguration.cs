using BudgetWise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetWise.Infrastructure.Persistence.Configurations;

public class UserCategoryExclusionConfiguration : IEntityTypeConfiguration<UserCategoryExclusion>
{
    public void Configure(EntityTypeBuilder<UserCategoryExclusion> builder)
    {
        builder.ToTable("user_category_exclusions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.HasIndex(e => new { e.UserId, e.CategoryId })
            .IsUnique()
            .HasDatabaseName("ix_user_category_exclusions_user_category");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();
    }
}
