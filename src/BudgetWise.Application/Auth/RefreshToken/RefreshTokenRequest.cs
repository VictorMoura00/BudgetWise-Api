namespace BudgetWise.Application.Auth.RefreshToken;

public sealed record RefreshTokenRequest(
    Guid UserId,
    string RefreshToken
);