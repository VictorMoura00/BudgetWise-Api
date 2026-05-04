using BudgetWise.Api.Extensions;
using BudgetWise.Application.Reports.DTOs;
using BudgetWise.Application.Reports.UseCases;
using BudgetWise.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BudgetWise.Api.Endpoints;

public class ReportEndpoints : IEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/reports")
            .WithTags("Reports")
            .RequireAuthorization();

        group.MapGet("/due-transactions", async (
            GetDueTransactionsReportUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken,
            [FromQuery] bool includeItems = false) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(userId, includeItems, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("GetDueTransactionsReport")
        .WithSummary("Relatório de vencimentos: vencidos, vencem hoje, próximos 7 e 30 dias, sem data de vencimento")
        .Produces<DueTransactionsReportResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/category-analysis", async (
            GetCategoryAnalysisReportUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken,
            [FromQuery] DateOnly? startDate = null,
            [FromQuery] DateOnly? endDate = null,
            [FromQuery] TransactionType? type = null,
            [FromQuery] bool? isConfirmed = null,
            [FromQuery] Guid? categoryId = null,
            [FromQuery] Guid? familyGroupId = null,
            [FromQuery] PaymentMethod? paymentMethod = null) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(
                userId, startDate, endDate, type, isConfirmed, categoryId, familyGroupId, paymentMethod, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("GetCategoryAnalysisReport")
        .WithSummary("Análise de gastos/receitas agrupada por categoria com percentuais")
        .Produces<CategoryAnalysisReportResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/payment-status", async (
            GetPaymentStatusReportUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken,
            [FromQuery] DateOnly? startDate = null,
            [FromQuery] DateOnly? endDate = null,
            [FromQuery] TransactionType? type = null,
            [FromQuery] Guid? categoryId = null,
            [FromQuery] Guid? familyGroupId = null,
            [FromQuery] PaymentMethod? paymentMethod = null) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(
                userId, startDate, endDate, type, categoryId, familyGroupId, paymentMethod, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("GetPaymentStatusReport")
        .WithSummary("Relatório de status de pagamento: confirmados, pendentes, vencidos, vencem hoje e próximos 7 dias")
        .Produces<PaymentStatusReportResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
