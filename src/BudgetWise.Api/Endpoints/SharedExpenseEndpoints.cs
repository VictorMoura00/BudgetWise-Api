using BudgetWise.Api.Extensions;
using BudgetWise.Api.Filters;
using BudgetWise.Application.SharedExpenses.DTOs;
using BudgetWise.Application.SharedExpenses.UseCases;
using System.Security.Claims;

namespace BudgetWise.Api.Endpoints;

public class SharedExpenseEndpoints : IEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/family-groups/{familyGroupId:guid}/shared-expenses")
            .WithTags("SharedExpenses")
            .RequireAuthorization();

        group.MapGet("/", async (
            Guid familyGroupId,
            GetSharedExpensesUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(familyGroupId, userId, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("GetSharedExpenses")
        .WithSummary("Lista despesas compartilhadas do grupo familiar")
        .Produces<IReadOnlyList<SharedExpenseResponse>>()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/summary", async (
            Guid familyGroupId,
            GetSharedExpenseSummaryUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(familyGroupId, userId, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("GetSharedExpensesSummary")
        .WithSummary("Retorna o resumo financeiro das despesas compartilhadas do grupo")
        .Produces<SharedExpenseSummaryResponse>()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", async (
            Guid familyGroupId,
            Guid id,
            GetSharedExpenseByIdUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(familyGroupId, id, userId, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("GetSharedExpenseById")
        .WithSummary("Retorna uma despesa compartilhada por ID")
        .Produces<SharedExpenseResponse>()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/", async (
            Guid familyGroupId,
            CreateSharedExpenseRequest request,
            CreateSharedExpenseUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(familyGroupId, request, userId, cancellationToken);
            return result.ToResponse(response =>
                Results.Created(
                    $"/api/v1/family-groups/{familyGroupId}/shared-expenses/{response.Id}",
                    response));
        })
        .WithName("CreateSharedExpense")
        .WithSummary("Cria uma despesa compartilhada vinculada ao grupo familiar")
        .AddEndpointFilter<ValidationFilter<CreateSharedExpenseRequest>>()
        .Produces<SharedExpenseResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/{id:guid}/participants/{participantUserId:guid}/settle", async (
            Guid familyGroupId,
            Guid id,
            Guid participantUserId,
            SettleSharedExpenseParticipantUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(
                familyGroupId, id, participantUserId, userId, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("SettleSharedExpenseParticipant")
        .WithSummary("Marca um participante como liquidado na despesa compartilhada")
        .Produces<SharedExpenseResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
