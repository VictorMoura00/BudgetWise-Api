using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bogus;
using BudgetWise.Application.Auth.Common;
using BudgetWise.Application.Auth.Register;
using BudgetWise.Application.Tags.DTOs;
using BudgetWise.Application.Transactions.DTOs;
using BudgetWise.Domain.Enums;
using BudgetWise.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace BudgetWise.IntegrationTests.Tags;

public sealed class TagEndpointsTests(BudgetWiseWebFactory factory)
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

    private async Task<TagResponse> CreateTagAsync(string token, string? name = null)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.PostAsJsonAsync("/api/v1/tags",
            new CreateTagRequest(name ?? _faker.Commerce.Department()));
        return (await response.Content.ReadFromJsonAsync<TagResponse>(JsonOptions))!;
    }

    private async Task<TransactionResponse> CreateTransactionAsync(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.PostAsJsonAsync("/api/v1/transactions",
            new CreateTransactionRequest(
                Description: _faker.Commerce.ProductName(),
                Amount: _faker.Random.Decimal(10, 5000),
                Type: TransactionType.Expense,
                TransactionDate: DateOnly.FromDateTime(DateTime.UtcNow),
                CategoryId: null,
                Notes: null,
                RecurrenceType: RecurrenceType.None,
                RecurrenceEndDate: null,
                IsConfirmed: false,
                PaymentMethod: PaymentMethod.Pix,
                FamilyGroupId: null));
        return (await response.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions))!;
    }

    // ── POST /tags ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidData_Returns201()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/v1/tags", new CreateTagRequest("alimentação"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<TagResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Id.Should().NotBeEmpty();
        body.Name.Should().Be("alimentação");
    }

    [Fact]
    public async Task Create_DuplicateName_Returns409()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync("/api/v1/tags", new CreateTagRequest("lazer"));
        var response = await _client.PostAsJsonAsync("/api/v1/tags", new CreateTagRequest("lazer"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_WithEmptyName_Returns400()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/v1/tags", new CreateTagRequest(""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithoutAuth_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/api/v1/tags", new CreateTagRequest("test"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /tags ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetList_ReturnsOnlyCurrentUserTags()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync("/api/v1/tags", new CreateTagRequest("tag-a"));
        await _client.PostAsJsonAsync("/api/v1/tags", new CreateTagRequest("tag-b"));

        var response = await _client.GetAsync("/api/v1/tags");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<IReadOnlyList<TagResponse>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetList_WithoutAuth_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/tags");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /tags/{id} ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WhenExists_Returns200()
    {
        var token = await AuthenticateAsync();
        var tag = await CreateTagAsync(token, "saúde");

        var response = await _client.GetAsync($"/api/v1/tags/{tag.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TagResponse>(JsonOptions);
        body!.Id.Should().Be(tag.Id);
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/v1/tags/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_WhenBelongsToAnotherUser_Returns404()
    {
        var tokenA = await AuthenticateAsync();
        var tag = await CreateTagAsync(tokenA, "transporte");

        var tokenB = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var response = await _client.GetAsync($"/api/v1/tags/{tag.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT /tags/{id} ────────────────────────────────────────────────────────

    [Fact]
    public async Task Rename_WithValidData_Returns200()
    {
        var token = await AuthenticateAsync();
        var tag = await CreateTagAsync(token, "compras");

        var response = await _client.PutAsJsonAsync($"/api/v1/tags/{tag.Id}",
            new UpdateTagRequest("supermercado"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TagResponse>(JsonOptions);
        body!.Name.Should().Be("supermercado");
    }

    [Fact]
    public async Task Rename_DuplicateName_Returns409()
    {
        var token = await AuthenticateAsync();
        await CreateTagAsync(token, "existente");
        var tag = await CreateTagAsync(token, "para-renomear");

        var response = await _client.PutAsJsonAsync($"/api/v1/tags/{tag.Id}",
            new UpdateTagRequest("existente"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Rename_WhenNotFound_Returns404()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PutAsJsonAsync($"/api/v1/tags/{Guid.NewGuid()}",
            new UpdateTagRequest("novo-nome"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /tags/{id} ─────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WhenExists_Returns204()
    {
        var token = await AuthenticateAsync();
        var tag = await CreateTagAsync(token);

        var response = await _client.DeleteAsync($"/api/v1/tags/{tag.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WhenNotFound_Returns404()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.DeleteAsync($"/api/v1/tags/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /transactions/{id}/tags/{tagId} ──────────────────────────────────

    [Fact]
    public async Task LinkTag_WithValidIds_Returns204()
    {
        var token = await AuthenticateAsync();
        var tag = await CreateTagAsync(token);
        var transaction = await CreateTransactionAsync(token);

        var response = await _client.PostAsync(
            $"/api/v1/transactions/{transaction.Id}/tags/{tag.Id}", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task LinkTag_TransactionGetById_ReturnsTagInResponse()
    {
        var token = await AuthenticateAsync();
        var tag = await CreateTagAsync(token, "investimento");
        var transaction = await CreateTransactionAsync(token);

        await _client.PostAsync($"/api/v1/transactions/{transaction.Id}/tags/{tag.Id}", null);

        var response = await _client.GetAsync($"/api/v1/transactions/{transaction.Id}");
        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions);

        body!.Tags.Should().ContainSingle(t => t.Id == tag.Id && t.Name == "investimento");
    }

    [Fact]
    public async Task LinkTag_AlreadyLinked_Returns409()
    {
        var token = await AuthenticateAsync();
        var tag = await CreateTagAsync(token);
        var transaction = await CreateTransactionAsync(token);

        await _client.PostAsync($"/api/v1/transactions/{transaction.Id}/tags/{tag.Id}", null);
        var response = await _client.PostAsync(
            $"/api/v1/transactions/{transaction.Id}/tags/{tag.Id}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task LinkTag_TransactionNotFound_Returns404()
    {
        var token = await AuthenticateAsync();
        var tag = await CreateTagAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsync(
            $"/api/v1/transactions/{Guid.NewGuid()}/tags/{tag.Id}", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LinkTag_TagNotFound_Returns404()
    {
        var token = await AuthenticateAsync();
        var transaction = await CreateTransactionAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsync(
            $"/api/v1/transactions/{transaction.Id}/tags/{Guid.NewGuid()}", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /transactions/{id}/tags/{tagId} ────────────────────────────────

    [Fact]
    public async Task UnlinkTag_WhenLinked_Returns204()
    {
        var token = await AuthenticateAsync();
        var tag = await CreateTagAsync(token);
        var transaction = await CreateTransactionAsync(token);

        await _client.PostAsync($"/api/v1/transactions/{transaction.Id}/tags/{tag.Id}", null);
        var response = await _client.DeleteAsync(
            $"/api/v1/transactions/{transaction.Id}/tags/{tag.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UnlinkTag_WhenNotLinked_Returns404()
    {
        var token = await AuthenticateAsync();
        var tag = await CreateTagAsync(token);
        var transaction = await CreateTransactionAsync(token);

        var response = await _client.DeleteAsync(
            $"/api/v1/transactions/{transaction.Id}/tags/{tag.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnlinkTag_TransactionNotFound_Returns404()
    {
        var token = await AuthenticateAsync();
        var tag = await CreateTagAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.DeleteAsync(
            $"/api/v1/transactions/{Guid.NewGuid()}/tags/{tag.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
