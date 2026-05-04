namespace BudgetWise.Application.Interfaces;

public sealed record UserInfo(string FullName, string Email);

public interface IUserLookupService
{
    Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesByIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, UserInfo>> GetUserInfosByIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);
}
