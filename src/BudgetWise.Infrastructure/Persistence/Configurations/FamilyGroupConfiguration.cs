using BudgetWise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetWise.Infrastructure.Persistence.Configurations;

public class FamilyGroupConfiguration : IEntityTypeConfiguration<FamilyGroup>
{
    public void Configure(EntityTypeBuilder<FamilyGroup> builder)
    {
        builder.ToTable("family_groups");

        builder.HasKey(fg => fg.Id);

        builder.Property(fg => fg.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(fg => fg.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(fg => fg.Description)
            .HasColumnType("text");

        builder.Property(fg => fg.InviteCode)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(fg => fg.CreatedBy)
            .IsRequired();

        builder.Property(fg => fg.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(fg => fg.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        // Index
        builder.HasIndex(fg => fg.InviteCode)
            .IsUnique()
            .HasDatabaseName("ix_family_groups_invite_code");
    }
}
