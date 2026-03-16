namespace BudgetWise.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string email, string fullName);
    string GenerateRefreshToken();
}