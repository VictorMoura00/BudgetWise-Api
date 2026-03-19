using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetWise.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.TransactionDate)
            .IsRequired();

        builder.Property(t => t.Notes)
            .HasColumnType("text");

        builder.Property(t => t.RecurrenceType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(RecurrenceType.None)
            .IsRequired();

        builder.Property(t => t.RecurrenceEndDate);

        builder.Property(t => t.IsConfirmed)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(t => t.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(t => t.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(t => t.DeletedAt);

        // ----- Relationships -----

        builder.HasOne(t => t.Category)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.FamilyGroup)
            .WithMany(fg => fg.Transactions)
            .HasForeignKey(t => t.FamilyGroupId)
            .OnDelete(DeleteBehavior.SetNull);

        // ----- CHECK constraints -----

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_transactions_amount", "amount > 0");

            t.HasCheckConstraint("ck_transactions_type",
                "type IN ('Income', 'Expense')");

            t.HasCheckConstraint("ck_transactions_recurrence_type",
                "recurrence_type IN ('None', 'Daily', 'Weekly', 'Monthly', 'Yearly')");

            t.HasCheckConstraint("ck_transactions_payment_method",
                "payment_method IN ('Pix', 'CreditCard', 'DebitCard', 'Cash', 'Ted', 'Boleto', 'Other') OR payment_method IS NULL");
        });

        // ----- Indexes -----

        builder.HasIndex(t => new { t.UserId, t.TransactionDate })
            .IsDescending(false, true)
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_transactions_user_date");

        builder.HasIndex(t => new { t.UserId, t.Type })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_transactions_user_type");

        builder.HasIndex(t => t.CategoryId)
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_transactions_category");

        builder.HasIndex(t => t.FamilyGroupId)
            .HasFilter("family_group_id IS NOT NULL AND deleted_at IS NULL")
            .HasDatabaseName("ix_transactions_family_group");

        builder.Ignore(t => t.IsDeleted);
    }
}
