using BudgetWise.Api.Extensions;
using BudgetWise.Api.Filters;
using BudgetWise.Application.Categories.DTOs;
using BudgetWise.Application.Categories.UseCases;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BudgetWise.Api.Endpoints;

public class CategoryEndpoints : IEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/categories")
            .WithTags("Categories")
            .RequireAuthorization();

        group.MapGet("/", async (
            [FromQuery] int pageNumber,
            [FromQuery] int pageSize,
            GetCategoriesUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var request = new GetCategoriesRequest(pageNumber < 1 ? 1 : pageNumber, pageSize < 1 ? 20 : pageSize);
            var result = await useCase.ExecuteAsync(request, userId, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("GetCategories")
        .WithSummary("Lista categorias do sistema e pessoais do usuário")
        .Produces<PaginatedCategoryResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", async (
            Guid id,
            GetCategoryByIdUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(id, userId, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("GetCategoryById")
        .WithSummary("Retorna uma categoria por ID")
        .Produces<CategoryResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/", async (
            CreateCategoryRequest request,
            CreateCategoryUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(request, userId, cancellationToken);
            return result.ToResponse(response =>
                Results.Created($"/api/v1/categories/{response.Id}", response));
        })
        .WithName("CreateCategory")
        .WithSummary("Cria uma nova categoria pessoal")
        .AddEndpointFilter<ValidationFilter<CreateCategoryRequest>>()
        .Produces<CategoryResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateCategoryRequest request,
            UpdateCategoryUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(id, request, userId, cancellationToken);
            return result.ToResponse(Results.Ok);
        })
        .WithName("UpdateCategory")
        .WithSummary("Atualiza uma categoria pessoal")
        .AddEndpointFilter<ValidationFilter<UpdateCategoryRequest>>()
        .Produces<CategoryResponse>()
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapDelete("/{id:guid}", async (
            Guid id,
            DeactivateCategoryUseCase useCase,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await useCase.ExecuteAsync(id, userId, cancellationToken);
            return result.ToResponse(() => Results.NoContent());
        })
        .WithName("DeactivateCategory")
        .WithSummary("Desativa uma categoria pessoal (soft delete)")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

    }
}
