namespace BudgetWise.Domain.Entities;

public class TransactionTag
{
    public Guid TransactionId { get; private init; }
    public Guid TagId { get; private init; }
    public Transaction Transaction { get; private init; } = null!;
    public Tag Tag { get; private init; } = null!;

    private TransactionTag() { }

    public static TransactionTag Create(Guid transactionId, Guid tagId)
    {
        return new TransactionTag
        {
            TransactionId = transactionId,
            TagId = tagId
        };
    }
}