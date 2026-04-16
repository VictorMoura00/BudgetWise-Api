using BudgetWise.Api.Extensions;
using BudgetWise.Api.Filters;
using BudgetWise.Application.Admin.DTOs;
using BudgetWise.Application.Admin.UseCases;
using BudgetWise.Domain.Common.Pagination;
using System.Security.Claims;

namespace BudgetWise.Api.Endpoints;

public class AdminEndpoints : IEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/users")
            .WithTags("Admin")
            .RequireAuthorization("Admin");

        group.MapGet("/", async (
            int page,
            int pageSize,
            GetUsersUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecuteAsync(page, pageSize, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("AdminGetUsers")
        .WithSummary("Lista todos os usuários cadastrados (paginado)")
        .Produces<PaginatedList<AdminUserResponse>>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/{id:guid}", async (
            Guid id,
            GetUserByIdUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecuteAsync(id, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("AdminGetUserById")
        .WithSummary("Retorna detalhes de um usuário")
        .Produces<AdminUserDetailResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPatch("/{id:guid}", async (
            Guid id,
            UpdateUserRequest request,
            UpdateUserUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecuteAsync(id, request, cancellationToken);
            return result.ToResponse(() => Results.NoContent());
        })
        .WithName("AdminUpdateUser")
        .WithSummary("Edita nome e e-mail de um usuário")
        .AddEndpointFilter<ValidationFilter<UpdateUserRequest>>()
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPatch("/{id:guid}/toggle-status", async (
            Guid id,
            ToggleUserStatusUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecuteAsync(id, cancellationToken);
            return result.ToResponse(() => Results.NoContent());
        })
        .WithName("AdminToggleUserStatus")
        .WithSummary("Ativa ou desativa a conta de um usuário")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPatch("/{id:guid}/unlock", async (
            Guid id,
            UnlockUserUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var result = await useCase.ExecuteAsync(id, cancellationToken);
            return result.ToResponse(() => Results.NoContent());
        })
        .WithName("AdminUnlockUser")
        .WithSummary("Desbloqueia uma conta em lockout")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPatch("/{id:guid}/role", async (
            Guid id,
            SetUserRoleRequest request,
            SetUserRoleUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var requestingAdminId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(id, requestingAdminId, request, cancellationToken);
            return result.ToResponse(() => Results.NoContent());
        })
        .WithName("AdminSetUserRole")
        .WithSummary("Define o role de um usuário (User ou Admin)")
        .AddEndpointFilter<ValidationFilter<SetUserRoleRequest>>()
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
