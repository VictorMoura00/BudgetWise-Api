namespace BudgetWise.Application.Auth.Register;

public sealed record RegisterUserRequest(
    string FullName,
    string Email,
    string Password,
    string ConfirmPassword
);