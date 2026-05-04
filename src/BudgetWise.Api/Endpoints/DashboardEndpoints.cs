using BudgetWise.Api.Extensions;
using BudgetWise.Application.Dashboard.DTOs;
using BudgetWise.Application.Dashboard.UseCases;
using BudgetWise.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BudgetWise.Api.Endpoints;

public class DashboardEndpoints : IEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/dashboard")
            .WithTags("Dashboard")
            .RequireAuthorization();

        group.MapGet("/overview", async (
            GetDashboardOverviewUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken,
            [FromQuery] DateOnly? startDate = null,
            [FromQuery] DateOnly? endDate = null,
            [FromQuery] int? year = null,
            [FromQuery] int? month = null,
            [FromQuery] TransactionType? type = null,
            [FromQuery] bool? isConfirmed = null,
            [FromQuery] Guid? categoryId = null,
            [FromQuery] Guid? familyGroupId = null,
            [FromQuery] PaymentMethod? paymentMethod = null) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(
                userId, startDate, endDate, year, month,
                type, isConfirmed, categoryId, familyGroupId, paymentMethod,
                cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("GetDashboardOverview")
        .WithSummary("Visão geral do dashboard financeiro: resumo financeiro, pendências por vencimento, destaques de categorias, maior gasto e comparativo mensal")
        .Produces<DashboardOverviewResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
