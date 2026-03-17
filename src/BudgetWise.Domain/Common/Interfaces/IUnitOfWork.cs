using System.Threading;
using System.Threading.Tasks;

namespace BudgetWise.Domain.Common.Interfaces;

public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}