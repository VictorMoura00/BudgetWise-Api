using BudgetWise.Api.Extensions;
using BudgetWise.Api.Filters;
using BudgetWise.Application.Transactions.DTOs;
using BudgetWise.Application.Transactions.UseCases;
using BudgetWise.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BudgetWise.Application.Tags.DTOs;

namespace BudgetWise.Api.Endpoints;

public class TransactionEndpoints : IEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/transactions")
            .WithTags("Transactions")
            .RequireAuthorization();

        group.MapGet("/", async (
            GetTransactionsUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken,
            [FromQuery] int? pageNumber = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] TransactionType? type = null,
            [FromQuery] Guid? categoryId = null,
            [FromQuery] DateOnly? startDate = null,
            [FromQuery] DateOnly? endDate = null,
            [FromQuery] bool? isConfirmed = null) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var request = new GetTransactionsRequest(
                pageNumber is null or < 1 ? 1 : pageNumber.Value,
                pageSize is null or < 1 ? 20 : pageSize.Value,
                type, categoryId, startDate, endDate, isConfirmed);
            var result = await useCase.ExecuteAsync(request, userId, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("GetTransactions")
        .WithSummary("Lista transações do usuário com filtros e paginação")
        .Produces<PaginatedTransactionResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/dashboard", async (
            [FromQuery] DateOnly? startDate,
            [FromQuery] DateOnly? endDate,
            GetDashboardSummaryUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(userId, startDate, endDate, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("GetDashboardSummary")
        .WithSummary("Retorna KPIs consolidados para o dashboard: taxa de poupança, maior gasto, projeção do mês e comparativo por categoria")
        .Produces<DashboardSummaryResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/summary", async (
            [FromQuery] DateOnly? startDate,
            [FromQuery] DateOnly? endDate,
            GetTransactionSummaryUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(userId, startDate, endDate, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("GetTransactionSummary")
        .WithSummary("Retorna totais de receita, despesa, saldo e pendências no período")
        .Produces<TransactionSummaryResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/monthly-summary", async (
            [FromQuery] int months,
            GetMonthlySummaryUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(userId, months < 1 ? 6 : months, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("GetMonthlySummary")
        .WithSummary("Retorna evolução mensal de receitas e despesas (padrão: 6 meses)")
        .Produces<IReadOnlyList<MonthlySummaryResponse>>()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", async (
            Guid id,
            GetTransactionByIdUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(id, userId, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("GetTransactionById")
        .WithSummary("Retorna uma transação por ID")
        .Produces<TransactionResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/", async (
            CreateTransactionRequest request,
            CreateTransactionUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(request, userId, cancellationToken);
            return result.ToResponse(response =>
                Results.Created($"/api/v1/transactions/{response.Id}", response));
        })
        .WithName("CreateTransaction")
        .WithSummary("Cria uma nova transação")
        .AddEndpointFilter<ValidationFilter<CreateTransactionRequest>>()
        .Produces<TransactionResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateTransactionRequest request,
            UpdateTransactionUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(id, request, userId, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("UpdateTransaction")
        .WithSummary("Atualiza uma transação")
        .AddEndpointFilter<ValidationFilter<UpdateTransactionRequest>>()
        .Produces<TransactionResponse>()
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapDelete("/{id:guid}", async (
            Guid id,
            DeleteTransactionUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(id, userId, cancellationToken);
            return result.ToResponse(() => Results.NoContent());
        })
        .WithName("DeleteTransaction")
        .WithSummary("Exclui uma transação (soft delete)")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPatch("/{id:guid}/confirm", async (
            Guid id,
            ConfirmTransactionUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(id, userId, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("ConfirmTransaction")
        .WithSummary("Confirma uma transação pendente")
        .Produces<TransactionResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/{id:guid}/tags/{tagId:guid}", async (
            Guid id,
            Guid tagId,
            AddTagToTransactionUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(id, tagId, userId, cancellationToken);
            return result.ToResponse(() => Results.NoContent());
        })
        .WithName("AddTagToTransaction")
        .WithSummary("Vincula uma tag a uma transação")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapDelete("/{id:guid}/tags/{tagId:guid}", async (
            Guid id,
            Guid tagId,
            RemoveTagFromTransactionUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(id, tagId, userId, cancellationToken);
            return result.ToResponse(() => Results.NoContent());
        })
        .WithName("RemoveTagFromTransaction")
        .WithSummary("Desvincula uma tag de uma transação")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
