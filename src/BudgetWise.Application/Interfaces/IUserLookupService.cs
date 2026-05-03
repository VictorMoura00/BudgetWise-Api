namespace BudgetWise.Application.Interfaces;

public interface IUserLookupService
{
    Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesByIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);
}
