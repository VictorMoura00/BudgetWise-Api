using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Infrastructure.Persistence;

namespace BudgetWise.Infrastructure.Repositories.Common;

public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public async Task CommitAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);
}