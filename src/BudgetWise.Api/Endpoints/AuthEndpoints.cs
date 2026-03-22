using BudgetWise.Api.Filters;
using BudgetWise.Application.Auth.Common;
using BudgetWise.Application.Auth.Login;
using BudgetWise.Application.Auth.RefreshToken;
using BudgetWise.Application.Auth.Register;
using BudgetWise.Api.Extensions;

namespace BudgetWise.Api.Endpoints;

public class AuthEndpoints : IEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Auth")
            .WithDescription("Registro, autenticação e renovação de tokens.");

        group.MapPost("/register", async (
            RegisterUserRequest request,
            RegisterUserUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecuteAsync(request, cancellationToken);
            return result.ToResponse(response =>
                Results.Created($"/api/v1/auth/{response.UserId}", response));
        })
        .WithName("Register")
        .WithSummary("Cria uma nova conta de usuário")
        .WithDescription("Registra um novo usuário e retorna o par de tokens (access + refresh).")
        .AllowAnonymous()
        .AddEndpointFilter<ValidationFilter<RegisterUserRequest>>()
        .Produces<AuthResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", async (
            LoginUserRequest request,
            LoginUserUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecuteAsync(request, cancellationToken);
            return result.ToResponse(response => Results.Ok(response));
        })
        .WithName("Login")
        .WithSummary("Autentica um usuário e retorna tokens")
        .WithDescription("Valida credenciais e retorna o par de tokens. Conta bloqueada após 5 tentativas falhas.")
        .AllowAnonymous()
        .AddEndpointFilter<ValidationFilter<LoginUserRequest>>()
        .Produces<AuthResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", async (
            RefreshTokenRequest request,
            RefreshTokenUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecuteAsync(request, cancellationToken);
            return result.ToResponse(response => Results.Ok(response));
        })
        .WithName("RefreshToken")
        .WithSummary("Renova o par de tokens via refresh token")
        .WithDescription("Rotação obrigatória: o refresh token anterior é invalidado e um novo par é gerado.")
        .AllowAnonymous()
        .AddEndpointFilter<ValidationFilter<RefreshTokenRequest>>()
        .Produces<AuthResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

    }
}