using BudgetWise.Api.Extensions;
using BudgetWise.Application.Reports.DTOs;
using BudgetWise.Application.Reports.UseCases;
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
    }
}
