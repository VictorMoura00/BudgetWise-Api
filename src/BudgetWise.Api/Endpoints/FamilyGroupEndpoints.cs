using BudgetWise.Api.Extensions;
using BudgetWise.Api.Filters;
using BudgetWise.Application.FamilyGroups.DTOs;
using BudgetWise.Application.FamilyGroups.UseCases;
using System.Security.Claims;

namespace BudgetWise.Api.Endpoints;

public class FamilyGroupEndpoints : IEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/family-groups")
            .WithTags("FamilyGroups")
            .RequireAuthorization();

        group.MapGet("/", async (
            GetFamilyGroupsUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(userId, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("GetFamilyGroups")
        .WithSummary("Lista os grupos familiares do usuário")
        .Produces<IReadOnlyList<FamilyGroupSummaryResponse>>()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", async (
            Guid id,
            GetFamilyGroupByIdUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(id, userId, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("GetFamilyGroupById")
        .WithSummary("Retorna um grupo familiar por ID")
        .Produces<FamilyGroupResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/", async (
            CreateFamilyGroupRequest request,
            CreateFamilyGroupUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(request, userId, cancellationToken);
            return result.ToResponse(response =>
                Results.Created($"/api/v1/family-groups/{response.Id}", response));
        })
        .WithName("CreateFamilyGroup")
        .WithSummary("Cria um novo grupo familiar")
        .AddEndpointFilter<ValidationFilter<CreateFamilyGroupRequest>>()
        .Produces<FamilyGroupResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateFamilyGroupRequest request,
            UpdateFamilyGroupUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(id, request, userId, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("UpdateFamilyGroup")
        .WithSummary("Atualiza nome e descrição do grupo (somente owner)")
        .AddEndpointFilter<ValidationFilter<UpdateFamilyGroupRequest>>()
        .Produces<FamilyGroupResponse>()
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapDelete("/{id:guid}", async (
            Guid id,
            DeleteFamilyGroupUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(id, userId, cancellationToken);
            return result.ToResponse(() => Results.NoContent());
        })
        .WithName("DeleteFamilyGroup")
        .WithSummary("Exclui o grupo familiar (somente owner)")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/join", async (
            JoinFamilyGroupRequest request,
            JoinFamilyGroupUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(request, userId, cancellationToken);
            return result.ToResponse(response =>
                Results.Created($"/api/v1/family-groups/{response.Id}", response));
        })
        .WithName("JoinFamilyGroup")
        .WithSummary("Entra em um grupo via código de convite")
        .AddEndpointFilter<ValidationFilter<JoinFamilyGroupRequest>>()
        .Produces<FamilyGroupSummaryResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/{id:guid}/leave", async (
            Guid id,
            LeaveFamilyGroupUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(id, userId, cancellationToken);
            return result.ToResponse(() => Results.NoContent());
        })
        .WithName("LeaveFamilyGroup")
        .WithSummary("Sai do grupo familiar")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapDelete("/{id:guid}/members/{memberUserId:guid}", async (
            Guid id,
            Guid memberUserId,
            RemoveMemberUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(id, memberUserId, userId, cancellationToken);
            return result.ToResponse(() => Results.NoContent());
        })
        .WithName("RemoveFamilyGroupMember")
        .WithSummary("Remove um membro do grupo (somente owner)")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/{id:guid}/invite/regenerate", async (
            Guid id,
            RegenerateInviteCodeUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(id, userId, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("RegenerateInviteCode")
        .WithSummary("Regenera o código de convite do grupo (somente owner)")
        .Produces<FamilyGroupResponse>()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
