using Bogus;
using BudgetWise.Application.Auth.Login;
using BudgetWise.Application.Auth.RefreshToken;
using BudgetWise.Application.Auth.Register;
using BudgetWise.Application.Interfaces;

namespace BudgetWise.UnitTests.Common;

public static class AuthFaker
{
    private static readonly Faker Faker = new("pt_BR");

    public static RegisterUserRequest ValidRegisterRequest() => new(
        FullName: Faker.Name.FullName(),
        Email: Faker.Internet.Email(),
        Password: "Senha@123",
        ConfirmPassword: "Senha@123");

    public static LoginUserRequest ValidLoginRequest() => new(
        Email: Faker.Internet.Email(),
        Password: "Senha@123");

    public static RefreshTokenRequest ValidRefreshTokenRequest() => new(
        UserId: Guid.NewGuid(),
        RefreshToken: Faker.Random.AlphaNumeric(64));

    public static AuthUserDto AuthUserDto() => new(
        Id: Guid.NewGuid(),
        Email: Faker.Internet.Email(),
        FullName: Faker.Name.FullName());
}