using BudgetWise.Application.Identity;

namespace BudgetWise.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string email, string fullName, UserRole role);
    string GenerateRefreshToken();
}