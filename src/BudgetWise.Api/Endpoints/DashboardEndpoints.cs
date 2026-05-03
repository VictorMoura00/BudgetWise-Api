using BudgetWise.Api.Extensions;
using BudgetWise.Application.Dashboard.DTOs;
using BudgetWise.Application.Dashboard.UseCases;
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
            [FromQuery] int? year = null,
            [FromQuery] int? month = null) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(userId, year, month, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("GetDashboardOverview")
        .WithSummary("Visão geral do dashboard financeiro: resumo financeiro, pendências por vencimento, destaques de categorias, maior gasto e comparativo mensal")
        .Produces<DashboardOverviewResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
