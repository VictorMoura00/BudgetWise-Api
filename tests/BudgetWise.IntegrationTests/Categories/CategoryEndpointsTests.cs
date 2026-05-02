using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bogus;
using BudgetWise.Application.Auth.Common;
using BudgetWise.Application.Auth.DTOs;
using BudgetWise.Application.Categories.DTOs;
using BudgetWise.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace BudgetWise.IntegrationTests.Categories;

public sealed class CategoryEndpointsTests(BudgetWiseWebFactory factory)
    : IClassFixture<BudgetWiseWebFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client = factory.CreateClient();
    private readonly Faker _faker = new("pt_BR");

    private async Task<string> AuthenticateAsync()
    {
        var email = _faker.Internet.Email();
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterUserRequest(_faker.Name.FullName(), email, "Senha@123", "Senha@123"));
        var tokens = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return tokens!.AccessToken;
    }

    private async Task<CategoryResponse> CreateCategoryAsync(string token, string? name = null)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.PostAsJsonAsync("/api/v1/categories",
            new CreateCategoryRequest(
                name ?? _faker.Commerce.Department(),
                _faker.Lorem.Sentence(),
                "icon",
                "#FF5733"));
        return (await response.Content.ReadFromJsonAsync<CategoryResponse>(JsonOptions))!;
    }

    private async Task<CategoryResponse> GetFirstSystemCategoryAsync(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/categories?pageNumber=1&pageSize=20");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<PaginatedCategoryResponse>(JsonOptions);
        return body!.Items.First(c => c.IsSystem);
    }

    // ── GET /categories ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetList_Authenticated_ReturnsSystemAndPersonalCategories()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await CreateCategoryAsync(token, "minha-categoria");

        var response = await _client.GetAsync("/api/v1/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaginatedCategoryResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Items.Count.Should().BeGreaterThanOrEqualTo(1);
        body.Items.Any(c => c.Name == "minha-categoria").Should().BeTrue();
    }

    [Fact]
    public async Task GetList_WithoutAuth_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/categories");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /categories/{id} ──────────────────────────────────────────────────

    [Fact]
    public async Task GetById_PersonalCategory_Returns200()
    {
        var token = await AuthenticateAsync();
        var category = await CreateCategoryAsync(token, "educação");

        var response = await _client.GetAsync($"/api/v1/categories/{category.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CategoryResponse>(JsonOptions);
        body!.Id.Should().Be(category.Id);
    }

    [Fact]
    public async Task GetById_SystemCategory_Returns200()
    {
        var token = await AuthenticateAsync();
        var systemCategory = await GetFirstSystemCategoryAsync(token);

        var response = await _client.GetAsync($"/api/v1/categories/{systemCategory.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CategoryResponse>(JsonOptions);
        body!.Id.Should().Be(systemCategory.Id);
        body.IsSystem.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/v1/categories/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_WhenBelongsToAnotherUser_Returns404()
    {
        var tokenA = await AuthenticateAsync();
        var category = await CreateCategoryAsync(tokenA, "outro-usuario");

        var tokenB = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var response = await _client.GetAsync($"/api/v1/categories/{category.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /categories ──────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidData_Returns201()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/v1/categories",
            new CreateCategoryRequest("moradia", "Gastos com casa", "home", "#3366FF"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CategoryResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Id.Should().NotBeEmpty();
        body.Name.Should().Be("moradia");
        body.IsSystem.Should().BeFalse();
    }

    [Fact]
    public async Task Create_DuplicateName_Returns409()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync("/api/v1/categories",
            new CreateCategoryRequest("transporte", null, null, null));
        var response = await _client.PostAsJsonAsync("/api/v1/categories",
            new CreateCategoryRequest("transporte", null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_WithoutAuth_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/api/v1/categories",
            new CreateCategoryRequest("teste", null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── PUT /categories/{id} ──────────────────────────────────────────────────

    [Fact]
    public async Task Update_WithValidData_Returns200()
    {
        var token = await AuthenticateAsync();
        var category = await CreateCategoryAsync(token, "alimentação");

        var response = await _client.PutAsJsonAsync($"/api/v1/categories/{category.Id}",
            new UpdateCategoryRequest("supermercado", "Compras no mercado", "cart", "#00FF00"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CategoryResponse>(JsonOptions);
        body!.Name.Should().Be("supermercado");
    }

    [Fact]
    public async Task Update_SystemCategory_Returns400()
    {
        var token = await AuthenticateAsync();
        var systemCategory = await GetFirstSystemCategoryAsync(token);

        var response = await _client.PutAsJsonAsync($"/api/v1/categories/{systemCategory.Id}",
            new UpdateCategoryRequest("novo-nome", null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_WhenBelongsToAnotherUser_Returns404()
    {
        var tokenA = await AuthenticateAsync();
        var category = await CreateCategoryAsync(tokenA, "lazer");

        var tokenB = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var response = await _client.PutAsJsonAsync($"/api/v1/categories/{category.Id}",
            new UpdateCategoryRequest("outro-nome", null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_DuplicateName_Returns409()
    {
        var token = await AuthenticateAsync();
        await CreateCategoryAsync(token, "categoria-a");
        var categoryB = await CreateCategoryAsync(token, "categoria-b");

        var response = await _client.PutAsJsonAsync($"/api/v1/categories/{categoryB.Id}",
            new UpdateCategoryRequest("categoria-a", null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── DELETE /categories/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task Deactivate_PersonalCategory_Returns204()
    {
        var token = await AuthenticateAsync();
        var category = await CreateCategoryAsync(token, "viagem");

        var response = await _client.DeleteAsync($"/api/v1/categories/{category.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Deactivate_SystemCategory_Returns204()
    {
        var token = await AuthenticateAsync();
        var systemCategory = await GetFirstSystemCategoryAsync(token);

        var response = await _client.DeleteAsync($"/api/v1/categories/{systemCategory.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Deactivate_WhenBelongsToAnotherUser_Returns404()
    {
        var tokenA = await AuthenticateAsync();
        var category = await CreateCategoryAsync(tokenA, "saúde");

        var tokenB = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var response = await _client.DeleteAsync($"/api/v1/categories/{category.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deactivate_WhenNotFound_Returns404()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.DeleteAsync($"/api/v1/categories/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
