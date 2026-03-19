using BudgetWise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetWise.Infrastructure.Persistence.Configurations;

public class FamilyMemberConfiguration : IEntityTypeConfiguration<FamilyMember>
{
    public void Configure(EntityTypeBuilder<FamilyMember> builder)
    {
        builder.ToTable("family_members");

        builder.HasKey(fm => fm.Id);

        builder.Property(fm => fm.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(fm => fm.FamilyGroupId)
            .IsRequired();

        builder.Property(fm => fm.UserId)
            .IsRequired();

        builder.Property(fm => fm.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(fm => fm.JoinedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(fm => fm.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(fm => fm.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasOne(fm => fm.FamilyGroup)
            .WithMany(fg => fg.Members)
            .HasForeignKey(fm => fm.FamilyGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(fm => new { fm.FamilyGroupId, fm.UserId })
            .IsUnique()
            .HasDatabaseName("ix_family_members_group_user");

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_family_members_role",
            "role IN ('Owner', 'Member')"));
    }
}
