using BudgetWise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetWise.Infrastructure.Persistence.Configurations;

public class TransactionTagConfiguration : IEntityTypeConfiguration<TransactionTag>
{
    public void Configure(EntityTypeBuilder<TransactionTag> builder)
    {
        builder.ToTable("transaction_tags");

        // PK composta
        builder.HasKey(tt => new { tt.TransactionId, tt.TagId });

        // FK → transactions
        builder.HasOne(tt => tt.Transaction)
            .WithMany(t => t.TransactionTags)
            .HasForeignKey(tt => tt.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK → tags
        builder.HasOne(tt => tt.Tag)
            .WithMany(t => t.TransactionTags)
            .HasForeignKey(tt => tt.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
