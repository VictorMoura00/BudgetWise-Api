using BudgetWise.Application.Identity;
using BudgetWise.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BudgetWise.Infrastructure.Services;

public sealed class UserLookupService(UserManager<ApplicationUser> userManager) : IUserLookupService
{
    public async Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesByIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.ToHashSet();

        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        return await userManager.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, DisplayName = u.FullName != "" ? u.FullName : u.Email! })
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);
    }
}
