using BudgetWise.Api.Extensions;
using BudgetWise.Api.Filters;
using BudgetWise.Application.Tags.DTOs;
using BudgetWise.Application.Tags.UseCases;
using System.Security.Claims;

namespace BudgetWise.Api.Endpoints;

public class TagEndpoints : IEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tags")
            .WithTags("Tags")
            .RequireAuthorization();

        group.MapGet("/", async (
            GetTagsUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(userId, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("GetTags")
        .WithSummary("Lista todas as tags do usuário")
        .Produces<IReadOnlyList<TagResponse>>()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", async (
            Guid id,
            GetTagByIdUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(id, userId, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("GetTagById")
        .WithSummary("Retorna uma tag por ID")
        .Produces<TagResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/", async (
            CreateTagRequest request,
            CreateTagUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(request, userId, cancellationToken);
            return result.ToResponse(response =>
                Results.Created($"/api/v1/tags/{response.Id}", response));
        })
        .WithName("CreateTag")
        .WithSummary("Cria uma nova tag")
        .AddEndpointFilter<ValidationFilter<CreateTagRequest>>()
        .Produces<TagResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateTagRequest request,
            RenameTagUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(id, request, userId, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("RenameTag")
        .WithSummary("Renomeia uma tag")
        .AddEndpointFilter<ValidationFilter<UpdateTagRequest>>()
        .Produces<TagResponse>()
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapDelete("/{id:guid}", async (
            Guid id,
            DeleteTagUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(id, userId, cancellationToken);
            return result.ToResponse(() => Results.NoContent());
        })
        .WithName("DeleteTag")
        .WithSummary("Exclui uma tag")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
