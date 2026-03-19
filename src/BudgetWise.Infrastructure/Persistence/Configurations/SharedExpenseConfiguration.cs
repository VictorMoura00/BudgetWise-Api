using BudgetWise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetWise.Infrastructure.Persistence.Configurations;

public class SharedExpenseConfiguration : IEntityTypeConfiguration<SharedExpense>
{
    public void Configure(EntityTypeBuilder<SharedExpense> builder)
    {
        builder.ToTable("shared_expenses");

        builder.HasKey(se => se.Id);

        builder.Property(se => se.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(se => se.TransactionId)
            .IsRequired();

        builder.Property(se => se.FamilyGroupId)
            .IsRequired();

        builder.Property(se => se.Description)
            .HasMaxLength(255);

        builder.Property(se => se.TotalAmount)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(se => se.CreatedBy)
            .IsRequired();

        builder.Property(se => se.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(se => se.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        // FK → transactions
        builder.HasOne(se => se.Transaction)
            .WithMany(t => t.SharedExpenses)
            .HasForeignKey(se => se.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK → family_groups
        builder.HasOne(se => se.FamilyGroup)
            .WithMany(fg => fg.SharedExpenses)
            .HasForeignKey(se => se.FamilyGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_shared_expenses_total_amount",
            "total_amount > 0"));

        builder.HasIndex(se => se.FamilyGroupId)
            .HasDatabaseName("ix_shared_expenses_group");

        builder.HasIndex(se => se.TransactionId)
            .HasDatabaseName("ix_shared_expenses_transaction");
    }
}
