namespace BudgetWise.Application.Auth.Login;

public sealed record LoginUserRequest(
    string Email,
    string Password
);