using BudgetWise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetWise.Infrastructure.Persistence.Configurations;

public class SharedExpenseParticipantConfiguration : IEntityTypeConfiguration<SharedExpenseParticipant>
{
    public void Configure(EntityTypeBuilder<SharedExpenseParticipant> builder)
    {
        builder.ToTable("shared_expense_participants");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.SharedExpenseId)
            .IsRequired();

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.AmountOwed)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(p => p.IsSettled)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(p => p.SettledAt);

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        // FK → shared_expenses
        builder.HasOne(p => p.SharedExpense)
            .WithMany(se => se.Participants)
            .HasForeignKey(p => p.SharedExpenseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.SharedExpenseId, p.UserId })
            .IsUnique()
            .HasDatabaseName("ix_shared_expense_participants_expense_user");

        // CHECKs
        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_shared_expense_participants_amount",
                "amount_owed > 0");

            t.HasCheckConstraint(
                "ck_shared_expense_participants_settled",
                "(is_settled = false AND settled_at IS NULL) OR (is_settled = true AND settled_at IS NOT NULL)");
        });
    }
}
